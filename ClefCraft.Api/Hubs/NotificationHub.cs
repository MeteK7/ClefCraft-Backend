using Microsoft.AspNetCore.SignalR;

namespace ClefCraft.Api.Hubs
{
    public class NotificationHub:Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Connected");

            Console.WriteLine(
                $"Authenticated: {Context.User?.Identity?.IsAuthenticated}");

            Console.WriteLine(
                $"UserIdentifier: {Context.UserIdentifier}");

            Console.WriteLine(
                $"uid: {Context.User?.FindFirst("uid")?.Value}");

            await base.OnConnectedAsync();
        }
    }
}
