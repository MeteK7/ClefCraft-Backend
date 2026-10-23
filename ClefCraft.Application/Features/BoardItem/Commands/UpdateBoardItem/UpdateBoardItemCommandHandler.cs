using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem
{
    public class UpdateBoardItemCommandHandler : IRequestHandler<UpdateBoardItemCommand, BoardItemByIdDto>
    {
        public const string ConflictMessage = "This item changed while you were saving it; reload it and try again.";

        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IStatusRepository _statusRepository;
        private readonly IPriorityRepository _priorityRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserService _userService;
        private readonly ITaskLifecycleService _taskLifecycleService;
        private readonly IUnitOfWork _unitOfWork; // Added for architectural parity
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly INotificationHubService _notificationHubService;

        public UpdateBoardItemCommandHandler(
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IStatusRepository statusRepository,
            IPriorityRepository priorityRepository,
            ITagRepository tagRepository,
            IUserService userService,
            ITaskLifecycleService taskLifecycleService,
            IUnitOfWork unitOfWork,
            IBoardMemberRepository boardMemberRepository,
            IBoardItemAskRepository askRepository,
            IBoardRepository boardRepository,
            IActivityLogger activityLogger,
            INotificationHubService notificationHubService)
        {
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _statusRepository = statusRepository;
            _priorityRepository = priorityRepository;
            _tagRepository = tagRepository;
            _userService = userService;
            _taskLifecycleService = taskLifecycleService;
            _unitOfWork = unitOfWork;
            _boardMemberRepository = boardMemberRepository;
            _askRepository = askRepository;
            _boardRepository = boardRepository;
            _activityLogger = activityLogger;
            _notificationHubService = notificationHubService;
        }

        public async Task<BoardItemByIdDto> Handle(UpdateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);

            if (boardItem == null)
            {
                throw new NotFoundException(nameof(BoardItem), request.Id);
            }

            await _boardAccessService.EnsureBoardOwnedByUserAsync(boardItem.BoardId, _userService.UserId);

            // null leaves the assignee as it is (partial updates such as SwitchColumn omit it);
            // "" or whitespace clears it.
            var newAssigneeId = request.AssigneeId == null
                ? boardItem.AssigneeId
                : string.IsNullOrWhiteSpace(request.AssigneeId) ? null : request.AssigneeId;

            // Only a *change* of assignee is checked, so an existing assignee who has since left
            // the board doesn't block unrelated edits to the item.
            if (newAssigneeId != null
                && newAssigneeId != boardItem.AssigneeId
                && !await _boardMemberRepository.IsMemberAsync(boardItem.BoardId, newAssigneeId))
            {
                throw new BadRequestException("The assignee must be a member of this board.");
            }

            // Moving the item (edit or SwitchColumn) must stay within its own board. An unchanged
            // column isn't re-checked, so existing items keep saving as they are.
            if (request.BoardColumnId != boardItem.BoardColumnId
                && !await _boardItemRepository.IsColumnOnBoardAsync(boardItem.BoardId, request.BoardColumnId))
            {
                throw new BadRequestException("The column does not belong to this board.");
            }

            var previousStatusId = boardItem.BoardItemStatus?.StatusId;
            var previousAssignee = boardItem.AssigneeId;
            var callerId = _userService.UserId;

            // Resolve "Done" by name: status ids depend on the order the statuses were created in.
            // Looked up only when the status changes.
            var statusChanges = request.StatusId.HasValue && request.StatusId != previousStatusId;
            var doneStatusIds = statusChanges ? await CompletedStatus.GetIdsAsync(_statusRepository) : null;
            var entersDone = doneStatusIds != null
                && doneStatusIds.Contains(request.StatusId!.Value)
                && !(previousStatusId.HasValue && doneStatusIds.Contains(previousStatusId.Value));

            // Completing the item settles its asks. This is the only place an item's status can move
            // to Done once it exists (PUT /BoardItems/{id} and SwitchColumn both come here), so no
            // path skips the required-ask rule. Validated before the item is touched.
            var askClosure = AskClosureResult.None;
            if (entersDone)
            {
                var asks = await _askRepository.GetByItemIdAsync(boardItem.Id);

                // The owner only matters for marking someone else's ask done (managers may).
                var boardOwnerId = request.AskClosure?.CompleteAskIds?.Count > 0
                    ? (await _boardRepository.GetByIdReadOnlyAsync(boardItem.BoardId))?.OwnerUserId
                    : null;

                askClosure = AskClosure.Apply(
                    asks,
                    request.AskClosure,
                    ask => AskActorRoles.For(ask, callerId, previousAssignee, boardOwnerId),
                    callerId,
                    DateTime.UtcNow);

                // Marked done on the recipient's behalf: the answer is their latest reply, as when
                // they mark it done themselves without one.
                var completedReplies = askClosure.Completed.Count > 0
                    ? await _askRepository.GetRepliesByAskIdsReadOnlyAsync(askClosure.Completed.Select(a => a.Id))
                    : new List<Domain.BoardItemAskReply>();

                foreach (var ask in askClosure.Completed)
                {
                    ask.AnswerReplyId = AskReplies.LatestAnswerCandidate(ask, completedReplies)?.Id;

                    await AskHistory.LogAsync(_activityLogger, AskHistory.Completed, ask,
                        new Dictionary<string, object?> { [AskHistory.CompletedByUserIdKey] = callerId });
                }

                foreach (var ask in askClosure.Cancelled)
                {
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Cancelled, ask,
                        new Dictionary<string, object?> { [AskHistory.ReasonKey] = nameof(BoardItemAskCancelReason.ItemCompleted) });
                }

                if (askClosure.OverriddenAskIds.Count > 0)
                    await AskHistory.LogRequirementsOverriddenAsync(_activityLogger, boardItem.Id, askClosure.OverriddenAskIds);

                // Always, even without asks: an ask created while this save is in flight then fails
                // instead of leaving a Done item with an open ask.
                boardItem.AskVersion++;
            }

            boardItem.Title = request.Title ?? boardItem.Title;
            boardItem.Description = request.Description ?? boardItem.Description;

            if (request.StatusId.HasValue)
            {
                if (boardItem.BoardItemStatus == null)
                {
                    boardItem.BoardItemStatus = new BoardItemStatus
                    {
                        BoardItemId = boardItem.Id,
                        StatusId = request.StatusId.Value
                    };
                }
                else
                {
                    boardItem.BoardItemStatus.StatusId = request.StatusId.Value;
                }
            }

            if (request.PriorityId.HasValue)
            {
                if (boardItem.BoardItemPriority == null)
                {
                    boardItem.BoardItemPriority = new BoardItemPriority
                    {
                        BoardItemId = boardItem.Id,
                        PriorityId = request.PriorityId.Value
                    };
                }
                else
                {
                    boardItem.BoardItemPriority.PriorityId = request.PriorityId.Value;
                }
            }

            // Handle tag updates
            if (request.TagIds != null)
            {
                var existingTagIds = boardItem.BoardItemTags.Select(t => t.TagId).ToList();

                var tagsToAdd = request.TagIds.Except(existingTagIds).ToList();
                var tagsToRemove = existingTagIds.Except(request.TagIds).ToList();

                boardItem.BoardItemTags
                    .Where(t => tagsToRemove.Contains(t.TagId))
                    .ToList()
                    .ForEach(t => boardItem.BoardItemTags.Remove(t));

                foreach (var tagId in tagsToAdd)
                {
                    boardItem.BoardItemTags.Add(new BoardItemTag
                    {
                        BoardItemId = boardItem.Id,
                        TagId = tagId
                    });
                }
            }

            // Update the rest of the properties
            boardItem.AssigneeId = newAssigneeId;
            boardItem.DueDate = request.DueDate ?? boardItem.DueDate;
            boardItem.EstimatedTime = request.EstimatedTime ?? boardItem.EstimatedTime;
            boardItem.TimeSpent = request.TimeSpent ?? boardItem.TimeSpent;
            boardItem.BoardColumnId = request.BoardColumnId;

            // Update state in change tracker
            await _boardItemRepository.UpdateBoardItem(boardItem);

            // Fix 3: Persist changes to database via Unit of Work. The item, its asks and their
            // history entries are written by this one save, so they succeed or fail together.
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new BadRequestException(ConflictMessage);
            }

            // Lifecycle tracking (Fix 2: Compare previous state with the newly mutated domain state)
            await _taskLifecycleService.RecordStatusChangeAsync(boardItem.Id);

            if (previousAssignee != boardItem.AssigneeId)
            {
                await _taskLifecycleService.RecordAssigneeChangeAsync(boardItem.Id);
            }

            var currentStatusId = boardItem.BoardItemStatus?.StatusId;

            if (previousStatusId != currentStatusId && doneStatusIds != null)
            {
                var wasDone = previousStatusId.HasValue && doneStatusIds.Contains(previousStatusId.Value);
                var isDone = currentStatusId.HasValue && doneStatusIds.Contains(currentStatusId.Value);

                if (isDone)
                    await _taskLifecycleService.RecordCompletionAsync(boardItem.Id);

                if (wasDone && !isDone)
                    await _taskLifecycleService.RecordReopenAsync(boardItem.Id);
            }

            // Commit lifecycle logs
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await AskNotifier.SendAsync(
                askClosure.Completed
                    .Select(ask => new AskNotice(ask, boardItem.BoardId, boardItem.Title, AskEvent.Completed))
                    .Concat(askClosure.Cancelled
                        .Select(ask => new AskNotice(ask, boardItem.BoardId, boardItem.Title, AskEvent.Cancelled,
                            nameof(BoardItemAskCancelReason.ItemCompleted))))
                    .ToList(),
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            // Reload fresh state with navigation properties
            var updatedItem = (await _boardItemRepository.GetBoardItemById(boardItem.Id))!; // just saved
            var dto = BoardItemMapper.ToByIdDto(updatedItem);

            // Populate assignee manually
            if (!string.IsNullOrEmpty(updatedItem.AssigneeId))
            {
                var assignee = await _userService.GetAssignee(updatedItem.AssigneeId);

                if (assignee != null)
                {
                    dto.AssigneeFirstName = assignee.Firstname;
                    dto.AssigneeLastName = assignee.Lastname;
                }
            }

            return dto;
        }
    }
}