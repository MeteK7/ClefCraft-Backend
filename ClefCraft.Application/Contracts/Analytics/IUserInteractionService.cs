using ClefCraft.Application.Models.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Analytics
{
    public interface IUserInteractionService
    {
        Task TrackAsync(string signalType, string entityType, int entityId, double value = 1);
        Task TrackBatchAsync(IEnumerable<Interaction> interactions);
    }
}
