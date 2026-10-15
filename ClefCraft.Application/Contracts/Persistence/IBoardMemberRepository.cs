using ClefCraft.Domain;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardMemberRepository : IGenericRepository<BoardMember>
    {
        Task<bool> IsMemberAsync(int boardId, string userId);

        Task<List<BoardMember>> GetByBoardIdAsync(int boardId);

        /// <summary>The members of all the given boards, in one query.</summary>
        Task<List<BoardMember>> GetByBoardIdsAsync(IEnumerable<int> boardIds);

        Task<BoardMember?> GetByBoardAndUserAsync(int boardId, string userId);

        Task<List<int>> GetMemberBoardIdsAsync(string userId);

        Task<bool> ShareAnyBoardAsync(string userId1, string userId2);
    }
}
