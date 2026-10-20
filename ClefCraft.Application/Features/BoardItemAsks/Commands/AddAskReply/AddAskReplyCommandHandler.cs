using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply
{
    public class AddAskReplyCommandHandler : IRequestHandler<AddAskReplyCommand, BoardItemAskDto>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly INotificationHubService _notificationHubService;
        private readonly IUnitOfWork _unitOfWork;

        public AddAskReplyCommandHandler(
            IBoardItemAskRepository askRepository,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository,
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
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
            _notificationHubService = notificationHubService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemAskDto> Handle(AddAskReplyCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new AddAskReplyCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid reply", validationResult);

            var callerId = _userService.UserId;

            var ask = await AskCommandSupport.LoadAskAsync(request.AskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            // Not an ask change: allowed while the item is Done, and neither Revision nor the
            // item's AskVersion moves.
            AskReplies.EnsureCanReply(ask, callerId);

            var reply = new BoardItemAskReply
            {
                BoardItemAskId = ask.Id,
                AuthorUserId = callerId,
                Body = request.Body.Trim()
            };

            await _askRepository.AddReplyAsync(reply);
            await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);

            var otherParty = callerId == ask.RecipientUserId ? ask.RequestedByUserId : ask.RecipientUserId;

            await AskNotifier.SendAsync(
                new[] { new AskNotice(ask, context.Item.BoardId, context.Item.Title, AskEvent.Replied, Audience: new[] { otherParty }, Reply: reply) },
                callerId, _notificationHubService, _boardMemberRepository, _userService, cancellationToken);

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
