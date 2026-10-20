using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk
{
    public class ReassignAskCommandHandler : IRequestHandler<ReassignAskCommand, BoardItemAskDto>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IActivityLogger _activityLogger;
        private readonly INotificationHubService _notificationHubService;
        private readonly IUnitOfWork _unitOfWork;

        public ReassignAskCommandHandler(
            IBoardItemAskRepository askRepository,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository,
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IActivityLogger activityLogger,
            INotificationHubService notificationHubService,
            IUnitOfWork unitOfWork)
        {
            _askRepository = askRepository;
            _boardItemRepository = boardItemRepository;
            _boardRepository = boardRepository;
            _statusRepository = statusRepository;
            _boardMemberRepository = boardMemberRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _activityLogger = activityLogger;
            _notificationHubService = notificationHubService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemAskDto> Handle(ReassignAskCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new ReassignAskCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid ask request", validationResult);

            var callerId = _userService.UserId;

            var old = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                old.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            AskTransitions.EnsureAllowed(BoardItemAskAction.Reassign, old, context.RolesFor(old, callerId), context.ItemIsDone);
            AskTransitions.EnsureRevision(old, request.ExpectedRevision);

            if (request.NewRecipientUserId == old.RecipientUserId)
                throw new BadRequestException("That person is already the one asked.");

            await AskCommandSupport.EnsureValidRecipientAsync(request.NewRecipientUserId, callerId, context.Item, _boardMemberRepository);

            var requirementMoves = old.IsRequired;

            old.State = BoardItemAskState.Cancelled;
            old.CancelReason = BoardItemAskCancelReason.Withdrawn;
            old.IsRequired = false;
            old.Revision++;

            var replacement = new BoardItemAsk
            {
                BoardItemId = old.BoardItemId,
                RequestedByUserId = old.RequestedByUserId,
                RecipientUserId = request.NewRecipientUserId,
                Note = request.Note?.Trim() ?? old.Note,
                DueDate = request.DueDate ?? old.DueDate,
                IsRequired = requirementMoves,
                // The same question to someone else still needs its answer.
                ExpectsAnswer = old.ExpectsAnswer,
                State = BoardItemAskState.Pending
            };

            await _askRepository.CreateAsync(replacement);

            await AskHistory.LogAsync(_activityLogger, AskHistory.Cancelled, old,
                new Dictionary<string, object?>
                {
                    [AskHistory.ReasonKey] = nameof(BoardItemAskCancelReason.Withdrawn),
                    [AskHistory.ToRecipientUserIdKey] = replacement.RecipientUserId
                });
            await AskHistory.LogAsync(_activityLogger, AskHistory.Created, replacement);

            if (requirementMoves)
                await AskHistory.LogRequirementMovedAsync(_activityLogger, old, replacement.RecipientUserId);

            // A new open ask: serializes against the item being completed at the same time.
            context.Item.AskVersion++;

            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            await AskNotifier.SendAsync(
                new[]
                {
                    new AskNotice(old, context.Item.BoardId, context.Item.Title, AskEvent.Cancelled,
                        nameof(BoardItemAskCancelReason.Withdrawn), Audience: new[] { old.RecipientUserId }),
                    new AskNotice(replacement, context.Item.BoardId, context.Item.Title, AskEvent.Requested,
                        Audience: new[] { replacement.RecipientUserId })
                },
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(replacement, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
