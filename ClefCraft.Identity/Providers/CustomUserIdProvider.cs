using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Identity.Providers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Extract the user identity id ("uid") instead of the default username string ("sub")
            return connection.User?.FindFirst("uid")?.Value;
        }
    }
}
