namespace ClefCraft.Identity.Models
{
    /// <summary>
    /// A rotating refresh token. Only a SHA-256 hash of the raw token is stored — the raw value
    /// exists only in the login/refresh response, so a database leak yields no usable tokens.
    /// Each successful refresh revokes this row and issues a new one (<see cref="ReplacedByTokenHash"/>);
    /// presenting an already-revoked token is treated as theft and revokes all of the user's tokens.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>Idle deadline: a token not refreshed by then is dead (the server-side "screen lock").</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Hard cap set at login and carried unchanged through every rotation.</summary>
        public DateTime SessionExpiresAt { get; set; }

        /// <summary>Concurrency token: two concurrent refreshes of the same token can't both rotate it.</summary>
        public DateTime? RevokedAt { get; set; }

        public string? ReplacedByTokenHash { get; set; }
    }
}
