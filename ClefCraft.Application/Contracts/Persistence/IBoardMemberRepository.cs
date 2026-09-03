using ClefCraft.Domain;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardMemberRepository : IGenericRepository<BoardMember>
    {
        Task<bool> IsMemberAsync(int boardId, string userId);

        Task<List<BoardMember>> GetByBoardIdAsync(int boardId);

        Task<BoardMember?> GetByBoardAndUserAsync(int boardId, string userId);
    }
}
