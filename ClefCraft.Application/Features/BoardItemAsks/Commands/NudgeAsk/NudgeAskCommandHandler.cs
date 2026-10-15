using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk
{
    public class NudgeAskCommandHandler : IRequestHandler<NudgeAskCommand, BoardItemAskDto>
    {
        public const string AlreadyNudgedMessage = "This ask was already nudged in the last 24 hours.";
        public const string ConflictMessage = "Someone nudged or changed this ask at the same moment; reload it.";

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

        public NudgeAskCommandHandler(
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

        public async Task<BoardItemAskDto> Handle(NudgeAskCommand request, CancellationToken cancellationToken)
        {
            var callerId = _userService.UserId;

            var ask = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            AskTransitions.EnsureAllowed(BoardItemAskAction.Nudge, ask, context.RolesFor(ask, callerId), context.ItemIsDone);

            var utcNow = DateTime.UtcNow;

            if (!AskTransitions.IsNudgeDue(ask, utcNow))
                throw new BadRequestException(AlreadyNudgedMessage);

            // LastNudgedAt is a concurrency token, so of two nudges that both read the old value
            // only one can commit. State, Revision and DateCreated are deliberately untouched.
            ask.LastNudgedAt = utcNow;
            await AskHistory.LogAsync(_activityLogger, AskHistory.Nudged, ask);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new BadRequestException(ConflictMessage);
            }

            await AskNotifier.SendAsync(
                new[] { new AskNotice(ask, context.Item.BoardId, context.Item.Title, AskEvent.Nudged) },
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository);
        }
    }
}
