using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IAIDataRepository
    {
        Task<List<ActivityLog>> GetEventLogs(List<int> eventIds);
        Task<List<UserInteractionSignal>> GetEventSignals(List<int> eventIds);
        Task<List<TaskLifecycle>> GetTaskLifecycles(List<int> boardItemIds);
    }
}
