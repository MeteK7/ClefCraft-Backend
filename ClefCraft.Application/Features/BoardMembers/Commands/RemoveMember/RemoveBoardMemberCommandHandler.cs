using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember
{
    public class RemoveBoardMemberCommandHandler : IRequestHandler<RemoveBoardMemberCommand>
    {
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveBoardMemberCommandHandler(
            IBoardMemberRepository boardMemberRepository,
            IBoardRepository boardRepository,
            IBoardAccessService boardAccessService,
            IUnitOfWork unitOfWork)
        {
            _boardMemberRepository = boardMemberRepository;
            _boardRepository = boardRepository;
            _boardAccessService = boardAccessService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(RemoveBoardMemberCommand request, CancellationToken cancellationToken)
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

            await _boardMemberRepository.DeleteAsync(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
