using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardRepository : IGenericRepository<Board>
    {
        Task<List<Board>> GetBoards();
    }
}
