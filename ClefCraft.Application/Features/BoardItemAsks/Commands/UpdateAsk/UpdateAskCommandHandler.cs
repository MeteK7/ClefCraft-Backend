using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk
{
    public class UpdateAskCommandHandler : IRequestHandler<UpdateAskCommand, BoardItemAskDto>
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

        public UpdateAskCommandHandler(
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

        public async Task<BoardItemAskDto> Handle(UpdateAskCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new UpdateAskCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid ask request", validationResult);

            var callerId = _userService.UserId;

            var ask = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);
            var roles = context.RolesFor(ask, callerId);

            var note = request.Note.Trim();
            var termsChanged = note != ask.Note || request.DueDate != ask.DueDate;
            var requiredChanged = request.IsRequired != ask.IsRequired;

            if (!termsChanged && !requiredChanged)
                return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);

            if (termsChanged)
                AskTransitions.EnsureAllowed(BoardItemAskAction.Edit, ask, roles, context.ItemIsDone);

            if (requiredChanged)
            {
                var action = request.IsRequired ? BoardItemAskAction.SetRequired : BoardItemAskAction.WaiveRequirement;
                AskTransitions.EnsureAllowed(action, ask, roles, context.ItemIsDone);
            }

            AskTransitions.EnsureRevision(ask, request.ExpectedRevision);

            if (termsChanged)
            {
                ask.Note = note;
                ask.DueDate = request.DueDate;

                // The recipient agreed to the old terms, so changed terms ask again. DateCreated
                // (the ask's age) is left alone.
                if (ask.State == BoardItemAskState.Accepted)
                {
                    ask.State = BoardItemAskState.Pending;
                    ask.RespondedAt = null;
                }

                await AskHistory.LogAsync(_activityLogger, AskHistory.Changed, ask,
                    new Dictionary<string, object?> { ["DueDate"] = ask.DueDate });
            }

            if (requiredChanged)
            {
                ask.IsRequired = request.IsRequired;

                if (ask.IsRequired)
                {
                    await AskHistory.LogAsync(_activityLogger, AskHistory.RequirementSet, ask);
                }
                else
                {
                    var reason = string.IsNullOrWhiteSpace(request.WaiveReason) ? null : request.WaiveReason.Trim();
                    await AskHistory.LogAsync(_activityLogger, AskHistory.RequirementWaived, ask,
                        new Dictionary<string, object?> { [AskHistory.ReasonKey] = reason });
                }
            }

            ask.Revision++;

            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            if (termsChanged)
            {
                await AskNotifier.SendAsync(
                    new[] { new AskNotice(ask, context.Item.BoardId, context.Item.Title, AskEvent.Changed) },
                    callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);
            }

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
