namespace ClefCraft.Persistence
{
    /// <summary>
    /// The database connection both DbContexts use (ConnectionStrings:ClefCraftDatabaseConnectionString).
    /// Registered only so the setting is validated at startup: without it the app would fail later,
    /// with a provider error from the startup migrations or the first request.
    /// </summary>
    public class DatabaseOptions
    {
        public const string ConnectionStringName = "ClefCraftDatabaseConnectionString";

        public string? ConnectionString { get; set; }
    }
}
