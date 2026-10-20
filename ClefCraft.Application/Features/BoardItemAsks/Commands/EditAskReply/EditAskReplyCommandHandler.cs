using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply
{
    public class EditAskReplyCommandHandler : IRequestHandler<EditAskReplyCommand, BoardItemAskDto>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public EditAskReplyCommandHandler(
            IBoardItemAskRepository askRepository,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository,
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _askRepository = askRepository;
            _boardItemRepository = boardItemRepository;
            _boardRepository = boardRepository;
            _statusRepository = statusRepository;
            _boardMemberRepository = boardMemberRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<BoardItemAskDto> Handle(EditAskReplyCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await new EditAskReplyCommandValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid reply", validationResult);

            var callerId = _userService.UserId;

            var reply = await _askRepository.GetReplyByIdAsync(request.ReplyId)
                ?? throw new NotFoundException(nameof(BoardItemAskReply), request.ReplyId);
            var ask = await AskCommandSupport.LoadAskAsync(reply.BoardItemAskId, _askRepository);
            var context = await AskCommandSupport.LoadItemAsync(
                ask.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            if (reply.AuthorUserId != callerId)
                throw new ForbiddenAccessException();

            if (ask.State == BoardItemAskState.Cancelled)
                throw new BadRequestException(AskReplies.CancelledMessage);

            var body = request.Body.Trim();

            // Saving the same text isn't an edit, so it doesn't mark the reply "edited".
            if (body != reply.Body)
            {
                reply.Body = body;
                reply.DateEdited = DateTime.UtcNow;
                await AskCommandSupport.SaveAsync(_unitOfWork, cancellationToken);
            }

            return await AskCommandSupport.ToDtoAsync(ask, context, callerId, _userService, _boardMemberRepository, _askRepository);
        }
    }
}
