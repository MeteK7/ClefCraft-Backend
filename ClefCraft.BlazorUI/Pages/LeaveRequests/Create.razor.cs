using ClefCraft.BlazorUI.Contracts;
using ClefCraft.BlazorUI.Models.LeaveRequests;
using ClefCraft.BlazorUI.Models.LeaveTypes;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Data;
using System.Net;

namespace ClefCraft.BlazorUI.Pages.LeaveRequests
{
    public partial class Create
    {
        [Inject] ILeaveTypeService leaveTypeService { get; set; }
        [Inject] ILeaveRequestService leaveRequestService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        LeaveRequestVM LeaveRequest { get; set; } = new LeaveRequestVM();
        List<LeaveTypeVM> leaveTypeVMs { get; set; } = new List<LeaveTypeVM>();

        protected override async Task OnInitializedAsync()
        {
            leaveTypeVMs = await leaveTypeService.GetLeaveTypes();
        }

        private async Task HandleValidSubmit()
        {
            // Perform form submission here
            await leaveRequestService.CreateLeaveRequest(LeaveRequest);

            // Obtain the authentication state
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // Check if the user is in the "Administrator" role
            if (user.IsInRole("Administrator"))
            {
                // Redirect to the admin index page
                NavigationManager.NavigateTo("/leaverequests/");
            }
            else
            {
                // Redirect to the employee index page
                NavigationManager.NavigateTo("/leaverequests/employeeindex");
            }
        }
    }
}
