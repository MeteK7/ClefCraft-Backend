using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IEventTypeRepository : IGenericRepository<EventType>
    {
        Task<List<EventType>> GetByUserIdAsync(string userId);

        /// <summary>The event types with these ids, in one query (any owner).</summary>
        Task<List<EventType>> GetByIdsAsync(List<int> ids);
    }
}
