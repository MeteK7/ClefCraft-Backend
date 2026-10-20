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

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk
{
    public class CreateAskCommandHandler : IRequestHandler<CreateAskCommand, BoardItemAskDto>
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

        public CreateAskCommandHandler(
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

        public async Task<BoardItemAskDto> Handle(CreateAskCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new CreateAskCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid ask request", validationResult);

            var callerId = _userService.UserId;

            var context = await AskCommandSupport.LoadItemAsync(
                request.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            if (context.ItemIsDone)
                throw new BadRequestException("Asks can't change while the item is Done.");

            BoardItemAsk? replaced = null;
            if (request.ReplacesAskId.HasValue)
            {
                replaced = await _askRepository.GetByIdAsync(request.ReplacesAskId.Value);

                if (replaced == null || replaced.BoardItemId != request.BoardItemId)
                    throw new BadRequestException("The ask to follow up isn't an ask on this item.");

                AskTransitions.EnsureAllowed(BoardItemAskAction.AskAgain, replaced, context.RolesFor(replaced, callerId), context.ItemIsDone);
            }

            await AskCommandSupport.EnsureValidRecipientAsync(request.RecipientUserId, callerId, context.Item, _boardMemberRepository);

            var ask = new BoardItemAsk
            {
                BoardItemId = context.Item.Id,
                RequestedByUserId = replaced?.RequestedByUserId ?? callerId,
                RecipientUserId = request.RecipientUserId,
                Note = request.Note.Trim(),
                DueDate = request.DueDate,
                IsRequired = request.IsRequired,
                ExpectsAnswer = request.ExpectsAnswer,
                State = BoardItemAskState.Pending
            };

            await _askRepository.CreateAsync(ask);
            await AskHistory.LogAsync(_activityLogger, AskHistory.Created, ask,
                ask.ExpectsAnswer ? new Dictionary<string, object?> { [AskHistory.ExpectsAnswerKey] = true } : null);

            if (replaced != null && replaced.IsRequired && ask.IsRequired)
            {
                replaced.IsRequired = false;
                replaced.Revision++;

                await AskHistory.LogRequirementMovedAsync(_activityLogger, replaced, ask.RecipientUserId);
            }

            // A new open ask: serializes against the item being completed at the same time.
            context.Item.AskVersion++;

            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            await AskNotifier.SendAsync(
                new[] { new AskNotice(ask, context.Item.BoardId, context.Item.Title, AskEvent.Requested) },
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
