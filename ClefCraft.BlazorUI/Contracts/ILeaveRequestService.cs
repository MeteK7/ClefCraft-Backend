using ClefCraft.BlazorUI.Models.LeaveRequests;
using ClefCraft.BlazorUI.Services.Base;

namespace ClefCraft.BlazorUI.Contracts
{
    public interface ILeaveRequestService
    {
        Task<AdminLeaveRequestViewVM> GetAdminLeaveRequestList();
        Task<EmployeeLeaveRequestViewVM> GetUserLeaveRequests();
        Task<Response<Guid>> CreateLeaveRequest(LeaveRequestVM leaveRequest);
        Task<LeaveRequestVM> GetLeaveRequest(int id);
        Task DeleteLeaveRequest(int id);
        Task<Response<Guid>> ApproveLeaveRequest(int id, bool approved);
        Task<Response<Guid>> CancelLeaveRequest(int id);
    }
}
