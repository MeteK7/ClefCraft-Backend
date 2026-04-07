using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Logging
{
    public interface IActivityLogger
    {
        Task LogAsync(
            string entityType,
            int entityId,
            string actionType,
            object? metadata = null
        );
    }
}
