using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Analytics
{
    public interface ITaskLifecycleService
    {
        Task EnsureCreatedAsync(int boardItemId);
        Task RecordFirstWorkAsync(int boardItemId);
        Task RecordCompletionAsync(int boardItemId);
        Task RecordReopenAsync(int boardItemId);
        Task RecordStatusChangeAsync(int boardItemId);
        Task RecordAssigneeChangeAsync(int boardItemId);
    }
}
