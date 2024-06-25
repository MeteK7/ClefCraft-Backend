using ClefCraft.BlazorUI.Contracts;
using ClefCraft.BlazorUI.Models.LeaveTypes;
using Microsoft.AspNetCore.Components;

namespace ClefCraft.BlazorUI.Pages.LeaveTypes
{
    public partial class Details
    {
        [Inject]
        ILeaveTypeService _client { get; set; }

        [Parameter]
        public int id { get; set; }

        LeaveTypeVM leaveType = new LeaveTypeVM();

        protected async override Task OnParametersSetAsync()
        {
            leaveType = await _client.GetLeaveTypeDetails(id);
        }
    }
}
