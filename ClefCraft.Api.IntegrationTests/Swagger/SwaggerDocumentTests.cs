using ClefCraft.Api.IntegrationTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Swashbuckle.AspNetCore.Swagger;

namespace ClefCraft.Api.IntegrationTests.Swagger
{
    // The Swagger UI is only served in Development, but the document is generated from the same
    // registration in every environment, so it can be checked here.
    public class SwaggerDocumentTests
    {
        private static OpenApiDocument GenerateV1()
        {
            using var factory = new ClefCraftApiFactory();
            return factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        }

        [Fact]
        public void Document_DescribesTheV1Api()
        {
            var document = GenerateV1();

            document.Info.Title.ShouldBe("ClefCraft API");
            document.Info.Version.ShouldBe("v1");
            document.Paths.ShouldContainKey("/api/Calendar/events");
        }

        [Fact]
        public void Document_RequiresTheBearerHeaderGlobally()
        {
            var document = GenerateV1();

            var scheme = document.Components!.SecuritySchemes!["Bearer"];
            scheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
            scheme.In.ShouldBe(ParameterLocation.Header);
            scheme.Name.ShouldBe("Authorization");

            var requirement = document.Security.ShouldHaveSingleItem();
            var reference = requirement.Keys.ShouldHaveSingleItem();
            reference.Reference.Id.ShouldBe("Bearer");
            reference.Target.ShouldNotBeNull();
        }
    }
}
