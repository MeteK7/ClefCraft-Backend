using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardItemRepository
    {
        Task<List<BoardColumn>> GetAllBoardColumnsWithItems();
        Task<List<BoardColumn>> GetBoardColumnsWithBoardItems(int boardId);
        Task<BoardItem> GetBoardItemById(int id);
        Task AddBoardItem(BoardItem BoardItem);
        Task UpdateBoardItem(BoardItem BoardItem);
    }
}
