namespace ClefCraft.Infrastructure.Services.AI
{
    /// <summary>
    /// Where the AI prediction service runs (AIService:BaseUrl). Validated at startup so a missing
    /// or relative URL fails there, not when the first calendar load creates the HttpClient.
    /// </summary>
    public class AIServiceOptions
    {
        public const string SectionName = "AIService";

        public string? BaseUrl { get; set; }

        // http(s) only: "localhost:8000" parses as an absolute URI with scheme "localhost", and on
        // Linux "/predict" parses as an absolute file URI.
        public bool HasAbsoluteBaseUrl() =>
            Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
