using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Domain;
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
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IStatusRepository _statusRepository;
        private readonly IPriorityRepository _priorityRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly ITaskLifecycleService _taskLifecycleService;
        private readonly IUnitOfWork _unitOfWork; // Added for architectural parity

        public UpdateBoardItemCommandHandler(
            IBoardItemRepository boardItemRepository,
            IBoardAccessService boardAccessService,
            IStatusRepository statusRepository,
            IPriorityRepository priorityRepository,
            ITagRepository tagRepository,
            IMapper mapper,
            IUserService userService,
            ITaskLifecycleService taskLifecycleService,
            IUnitOfWork unitOfWork)
        {
            _boardItemRepository = boardItemRepository;
            _boardAccessService = boardAccessService;
            _statusRepository = statusRepository;
            _priorityRepository = priorityRepository;
            _tagRepository = tagRepository;
            _mapper = mapper;
            _userService = userService;
            _taskLifecycleService = taskLifecycleService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemByIdDto> Handle(UpdateBoardItemCommand request, CancellationToken cancellationToken)
        {
            var boardItem = await _boardItemRepository.GetBoardItemById(request.Id);

            if (boardItem == null)
            {
                throw new ApplicationException($"Board item with ID {request.Id} not found.");
            }

            await _boardAccessService.EnsureBoardOwnedByUserAsync(boardItem.BoardId, _userService.UserId);

            var previousStatusId = boardItem.BoardItemStatus?.StatusId;
            var previousAssignee = boardItem.AssigneeId;

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
            boardItem.AssigneeId = request.AssigneeId ?? boardItem.AssigneeId;
            boardItem.DueDate = request.DueDate ?? boardItem.DueDate;
            boardItem.EstimatedTime = request.EstimatedTime ?? boardItem.EstimatedTime;
            boardItem.TimeSpent = request.TimeSpent ?? boardItem.TimeSpent;
            boardItem.BoardColumnId = request.BoardColumnId;

            // Update state in change tracker
            await _boardItemRepository.UpdateBoardItem(boardItem);

            // Fix 3: Persist changes to database via Unit of Work
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Lifecycle tracking (Fix 2: Compare previous state with the newly mutated domain state)
            await _taskLifecycleService.RecordStatusChangeAsync(boardItem.Id);

            if (previousAssignee != boardItem.AssigneeId)
            {
                await _taskLifecycleService.RecordAssigneeChangeAsync(boardItem.Id);
            }

            const int COMPLETED_STATUS_ID = 3;
            var currentStatusId = boardItem.BoardItemStatus?.StatusId;

            if (previousStatusId != currentStatusId)
            {
                if (currentStatusId == COMPLETED_STATUS_ID)
                    await _taskLifecycleService.RecordCompletionAsync(boardItem.Id);

                if (previousStatusId == COMPLETED_STATUS_ID && currentStatusId != COMPLETED_STATUS_ID)
                    await _taskLifecycleService.RecordReopenAsync(boardItem.Id);
            }

            // Commit lifecycle logs
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload fresh state with navigation properties
            var updatedItem = await _boardItemRepository.GetBoardItemById(boardItem.Id);
            var dto = _mapper.Map<BoardItemByIdDto>(updatedItem);

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