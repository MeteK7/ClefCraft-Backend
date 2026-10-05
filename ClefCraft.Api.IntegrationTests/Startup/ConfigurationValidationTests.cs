using ClefCraft.Api.IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;

namespace ClefCraft.Api.IntegrationTests.Startup
{
    // Program.cs validates the settings it can't run without before anything else (including the
    // startup migrations), in every environment. These start the real host with one setting broken.
    public class ConfigurationValidationTests
    {
        private sealed class MisconfiguredFactory : ClefCraftApiFactory
        {
            private readonly string _key;
            private readonly string _value;

            public MisconfiguredFactory(string key, string value)
            {
                _key = key;
                _value = value;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                // Added after the base factory's values, so this one wins.
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { [_key] = _value }));
            }
        }

        // Host startup can wrap the exception (e.g. in an AggregateException), so look inside.
        private static OptionsValidationException? FindValidationException(Exception? exception) => exception switch
        {
            null => null,
            OptionsValidationException validation => validation,
            AggregateException aggregate => aggregate.Flatten().InnerExceptions
                .Select(FindValidationException)
                .FirstOrDefault(found => found != null),
            _ => FindValidationException(exception.InnerException)
        };

        [Theory]
        [InlineData("JwtSettings:Key", "too-short-for-hmac-sha256", "JwtSettings:Key")]
        [InlineData("JwtSettings:Key", "", "JwtSettings:Key")]
        [InlineData("JwtSettings:Issuer", "", "JwtSettings:Issuer")]
        [InlineData("JwtSettings:Audience", "", "JwtSettings:Audience")]
        [InlineData("ConnectionStrings:ClefCraftDatabaseConnectionString", "", "ConnectionStrings:ClefCraftDatabaseConnectionString")]
        [InlineData("AIService:BaseUrl", "", "AIService:BaseUrl")]
        [InlineData("AIService:BaseUrl", "predict/batch", "AIService:BaseUrl")]
        [InlineData("AIService:BaseUrl", "localhost:8000", "AIService:BaseUrl")]
        public void Startup_FailsWithAValidationMessageNamingTheSetting(string key, string value, string expectedSetting)
        {
            using var factory = new MisconfiguredFactory(key, value);

            var thrown = Record.Exception(() => factory.CreateClient());

            var validation = FindValidationException(thrown);
            validation.ShouldNotBeNull($"expected an OptionsValidationException, got: {thrown}");
            validation.Message.ShouldContain(expectedSetting);
        }

        [Fact]
        public async Task Startup_WithValidSettings_ServesRequests()
        {
            using var factory = new ClefCraftApiFactory();
            var client = factory.CreateClient();

            using var response = await client.GetAsync("/api/Boards");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
