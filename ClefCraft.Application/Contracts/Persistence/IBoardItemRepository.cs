using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardItemRepository : IGenericRepository<BoardItem>
    {
        Task<List<BoardColumn>> GetAllBoardColumnsWithItems(string userId);
        Task<List<BoardColumn>> GetBoardColumnsWithBoardItems(int boardId);
        Task<BoardItem> GetBoardItemById(int id);
        Task<BoardItemStatus?> GetBoardItemStatusByBoardItemId(int boardItemId);
        Task<BoardItemPriority?> GetBoardItemPriorityByBoardItemId(int boardItemId);
        Task<List<BoardItemTag>> GetBoardItemTagsByBoardItemId(int boardItemId);
        Task<List<BoardItem>> GetByIdsAsync(List<int> ids);
        Task AddBoardItem(BoardItem BoardItem);
        Task UpdateBoardItem(BoardItem BoardItem);
    }
}
