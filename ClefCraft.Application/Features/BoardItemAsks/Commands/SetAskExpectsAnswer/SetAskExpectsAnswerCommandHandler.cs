using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.SetAskExpectsAnswer
{
    public class SetAskExpectsAnswerCommandHandler : IRequestHandler<SetAskExpectsAnswerCommand, BoardItemAskDto>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IActivityLogger _activityLogger;
        private readonly IUnitOfWork _unitOfWork;

        public SetAskExpectsAnswerCommandHandler(
            IBoardItemAskRepository askRepository,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository,
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IActivityLogger activityLogger,
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
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemAskDto> Handle(SetAskExpectsAnswerCommand request, CancellationToken cancellationToken)
        {
            var callerId = _userService.UserId;

            var ask = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            var action = request.ExpectsAnswer ? BoardItemAskAction.SetExpectsAnswer : BoardItemAskAction.ClearExpectsAnswer;
            AskTransitions.EnsureAllowed(action, ask, context.RolesFor(ask, callerId), context.ItemIsDone);
            AskTransitions.EnsureRevision(ask, request.ExpectedRevision);

            // As with Make required: a change to the ask's terms that the recipient sees on their
            // next load, recorded in the item's history, without a notification.
            ask.ExpectsAnswer = request.ExpectsAnswer;
            ask.Revision++;

            await AskHistory.LogAsync(_activityLogger,
                request.ExpectsAnswer ? AskHistory.AnswerExpected : AskHistory.AnswerNotExpected, ask);

            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
