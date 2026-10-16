using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState
{
    public class ChangeAskStateCommandHandler : IRequestHandler<ChangeAskStateCommand, BoardItemAskDto>
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

        public ChangeAskStateCommandHandler(
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

        public async Task<BoardItemAskDto> Handle(ChangeAskStateCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new ChangeAskStateCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid ask request", validationResult);

            var callerId = _userService.UserId;

            var ask = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            AskTransitions.EnsureAllowed(request.Action, ask, context.RolesFor(ask, callerId), context.ItemIsDone);
            AskTransitions.EnsureRevision(ask, request.ExpectedRevision);

            var utcNow = DateTime.UtcNow;
            string? reason = null;
            AskEvent askEvent;

            switch (request.Action)
            {
                case BoardItemAskAction.Accept:
                    ask.State = BoardItemAskState.Accepted;
                    ask.RespondedAt = utcNow;
                    askEvent = AskEvent.Accepted;
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Accepted, ask);
                    break;

                case BoardItemAskAction.Decline:
                    reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
                    ask.State = BoardItemAskState.Declined;
                    ask.RespondedAt = utcNow;
                    ask.ResponseNote = reason;
                    askEvent = AskEvent.Declined;
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Declined, ask,
                        new Dictionary<string, object?> { [AskHistory.ReasonKey] = reason });
                    break;

                case BoardItemAskAction.Complete:
                    ask.State = BoardItemAskState.Completed;
                    ask.CompletedAt = utcNow;
                    ask.CompletedByUserId = callerId;
                    askEvent = AskEvent.Completed;
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Completed, ask,
                        new Dictionary<string, object?> { [AskHistory.CompletedByUserIdKey] = callerId });
                    break;

                case BoardItemAskAction.Reopen:
                    ask.State = BoardItemAskState.Accepted;
                    ask.CompletedAt = null;
                    ask.CompletedByUserId = null;
                    askEvent = AskEvent.Reopened;
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Reopened, ask);

                    // The ask is open again: serializes against the item being completed meanwhile.
                    context.Item.AskVersion++;
                    break;

                case BoardItemAskAction.Cancel:
                    ask.State = BoardItemAskState.Cancelled;
                    ask.CancelReason = BoardItemAskCancelReason.Withdrawn;
                    reason = nameof(BoardItemAskCancelReason.Withdrawn);
                    askEvent = AskEvent.Cancelled;
                    await AskHistory.LogAsync(_activityLogger, AskHistory.Cancelled, ask,
                        new Dictionary<string, object?> { [AskHistory.ReasonKey] = reason });
                    break;

                default:
                    throw new BadRequestException($"{request.Action} isn't a state change.");
            }

            ask.Revision++;

            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            await AskNotifier.SendAsync(
                new[] { new AskNotice(ask, context.Item.BoardId, context.Item.Title, askEvent, reason) },
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository);
        }
    }
}
