using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardTaskRepository
    {
        Task<List<BoardColumn>> GetBoardColumnsWithBoardTasks();
        Task AddBoardTask(BoardTask boardTask);
    }
}
