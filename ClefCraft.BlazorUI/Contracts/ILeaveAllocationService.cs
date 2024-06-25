using ClefCraft.BlazorUI.Services.Base;

namespace ClefCraft.BlazorUI.Contracts
{
    public interface ILeaveAllocationService
    {
        Task<Response<Guid>> CreateLeaveAllocations(int leaveTypeId);
    }
}
