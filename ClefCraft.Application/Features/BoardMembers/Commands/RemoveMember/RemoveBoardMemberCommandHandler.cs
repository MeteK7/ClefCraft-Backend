using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember
{
    public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand>
    {
        public const string ConflictMessage = "One of this member's asks changed while they were being removed; try again.";

        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly INotificationHubService _notificationHubService;
        private readonly IUserService _userService;

        public RemoveBoardMemberCommandHandler(
            IBoardMemberRepository boardMemberRepository,
            IBoardRepository boardRepository,
            IBoardAccessService boardAccessService,
            IUnitOfWork unitOfWork,
            IBoardItemAskRepository askRepository,
            IActivityLogger activityLogger,
            INotificationHubService notificationHubService,
            IUserService userService)
        {
            _boardMemberRepository = boardMemberRepository;
            _boardRepository = boardRepository;
            _boardAccessService = boardAccessService;
            _unitOfWork = unitOfWork;
            _askRepository = askRepository;
            _activityLogger = activityLogger;
            _notificationHubService = notificationHubService;
            _userService = userService;
        }

        public async Task Handle(RemoveBoardMemberCommand request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureUserIsBoardOwnerAsync(request.BoardId, request.RequestingUserId);

            var board = await _boardRepository.GetByIdReadOnlyAsync(request.BoardId);
            if (board == null)
                throw new NotFoundException(nameof(Board), request.BoardId);

            if (board.OwnerUserId == request.UserId)
                throw new BadRequestException("The board owner cannot be removed from its own board.");

            var membership = await _boardMemberRepository.GetByBoardAndUserAsync(request.BoardId, request.UserId);
            if (membership == null)
                throw new NotFoundException(nameof(BoardMember), request.UserId);

            // Once removed they can't open these items, so their open asks can't be answered.
            // Cancelling doesn't satisfy a requirement: a required one stays unmet until a manager
            // moves, waives or overrides it.
            var openAsks = await _askRepository.GetOpenByRecipientOnBoardAsync(request.BoardId, request.UserId);
            var utcNow = DateTime.UtcNow;

            foreach (var ask in openAsks)
            {
                ask.State = BoardItemAskState.Cancelled;
                ask.CancelReason = BoardItemAskCancelReason.RecipientRemoved;
                ask.CancelledAt = utcNow;
                ask.CancelledByUserId = request.RequestingUserId;
                ask.Revision++;

                await AskHistory.LogAsync(_activityLogger, AskHistory.Cancelled, ask,
                    new Dictionary<string, object?> { [AskHistory.ReasonKey] = nameof(BoardItemAskCancelReason.RecipientRemoved) });
            }

            await _boardMemberRepository.DeleteAsync(membership);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new BadRequestException(ConflictMessage);
            }

            // The removed member is no longer a member, so only the requesters hear about it.
            await AskNotifier.SendAsync(
                openAsks
                    .Select(ask => new AskNotice(ask, request.BoardId, ask.BoardItem.Title, AskEvent.Cancelled,
                        nameof(BoardItemAskCancelReason.RecipientRemoved)))
                    .ToList(),
                request.RequestingUserId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);
        }
    }
}
