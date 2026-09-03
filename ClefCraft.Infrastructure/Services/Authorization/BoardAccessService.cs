using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;

namespace ClefCraft.Infrastructure.Services.Authorization
{
    public class BoardAccessService : IBoardAccessService
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;

        public BoardAccessService(
            IBoardRepository boardRepository,
            IBoardItemRepository boardItemRepository,
            IBoardMemberRepository boardMemberRepository)
        {
            _boardRepository = boardRepository;
            _boardItemRepository = boardItemRepository;
            _boardMemberRepository = boardMemberRepository;
        }

        public async Task EnsureBoardOwnedByUserAsync(int boardId, string userId)
        {
            var board = await _boardRepository.GetByIdReadOnlyAsync(boardId);

            if (board == null)
                throw new NotFoundException(nameof(Board), boardId);

            if (!await _boardMemberRepository.IsMemberAsync(boardId, userId))
                throw new ForbiddenAccessException();
        }

        public async Task EnsureBoardItemOwnedByUserAsync(int boardItemId, string userId)
        {
            var item = await _boardItemRepository.GetByIdReadOnlyAsync(boardItemId);

            if (item == null)
                throw new NotFoundException(nameof(BoardItem), boardItemId);

            await EnsureBoardOwnedByUserAsync(item.BoardId, userId);
        }

        public async Task EnsureUserIsBoardOwnerAsync(int boardId, string userId)
        {
            var board = await _boardRepository.GetByIdReadOnlyAsync(boardId);

            if (board == null)
                throw new NotFoundException(nameof(Board), boardId);

            if (board.OwnerUserId != userId)
                throw new ForbiddenAccessException();
        }
    }
}
