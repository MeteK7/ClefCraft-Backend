using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Models.Identity
{
    public class JwtSettings
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public double DurationInMinutes { get; set; }

        /// <summary>A refresh token unused for this long expires — the session's inactivity lock.</summary>
        public double RefreshTokenIdleMinutes { get; set; } = 15;

        /// <summary>Absolute session length from login, however active the user is.</summary>
        public double SessionMaxHours { get; set; } = 8;
    }
}
