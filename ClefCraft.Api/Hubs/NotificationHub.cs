using Microsoft.AspNetCore.SignalR;

namespace ClefCraft.Api.Hubs
{
    public class NotificationHub:Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}
