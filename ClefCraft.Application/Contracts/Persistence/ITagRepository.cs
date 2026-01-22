using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<List<Tag>> GetTags();
        Task<List<Tag>> GetTagsByIdsAsync(List<int> tagIds);
        Task<List<Tag>> GetTagsByBoardIdAsync(int boardId);
    }
}
