using ClefCraft.Api.IntegrationTests.TestHelpers;
using ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Attachments
{
    public class CalendarAttachmentEndpointTests
    {
        private static async Task<int> CreateEventAsync(HttpClient client)
        {
            using var response = await client.PostAsJsonAsync("/api/Calendar", new
            {
                subject = "Lesson",
                startDate = "2026-10-01T10:00:00Z",
                endDate = "2026-10-01T11:00:00Z",
                timeZoneId = "UTC"
            });
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<int>();
        }

        private static MultipartFormDataContent Files(params (string Name, byte[] Content)[] files)
        {
            var form = new MultipartFormDataContent();
            foreach (var (name, content) in files)
            {
                var part = new ByteArrayContent(content);
                // Whatever the uploader claims; the download must not echo it.
                part.Headers.ContentType = new MediaTypeHeaderValue("application/x-claimed-by-client");
                form.Add(part, "files", name);
            }
            return form;
        }

        private static async Task<JsonArray> UploadAsync(HttpClient client, int eventId, params (string, byte[])[] files)
        {
            using var response = await client.PostAsync($"/api/Calendar/{eventId}/attachments", Files(files));
            response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        }

        [Fact]
        public async Task UploadedFile_DownloadsIntact_AsAnAttachment_WithAServerChosenType()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("uploader");
            var eventId = await CreateEventAsync(client);
            var content = Encoding.UTF8.GetBytes("<script>alert('x')</script>");

            var uploaded = await UploadAsync(client, eventId, ("page.html", content));
            var id = uploaded.Single()!["id"]!.GetValue<int>();

            using var download = await client.GetAsync($"/api/Calendar/attachments/download/{id}");

            download.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await download.Content.ReadAsByteArrayAsync()).ShouldBe(content);
            download.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
            download.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
            download.Content.Headers.ContentDisposition.FileNameStar.ShouldBe("page.html");
            download.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");

            // Stored under the configured root, in the event's folder.
            Directory.GetFiles(Path.Combine(factory.AttachmentRoot, eventId.ToString())).Length.ShouldBe(1);
        }

        [Fact]
        public async Task UnknownExtension_IsServedAsOctetStream()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("uploader");
            var eventId = await CreateEventAsync(client);

            var id = (await UploadAsync(client, eventId, ("riff.gp5", new byte[] { 1, 2, 3 }))).Single()!["id"]!.GetValue<int>();

            using var download = await client.GetAsync($"/api/Calendar/attachments/download/{id}");
            download.Content.Headers.ContentType!.MediaType.ShouldBe("application/octet-stream");
        }

        [Fact]
        public async Task AttachmentResponses_NeverIncludeTheServerPath()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("uploader");
            var eventId = await CreateEventAsync(client);

            var uploaded = await UploadAsync(client, eventId, ("a.txt", new byte[] { 65 }));
            using var list = await client.GetAsync($"/api/Calendar/{eventId}/attachments");
            var listBody = await list.Content.ReadAsStringAsync();

            uploaded.Single()!.AsObject().ContainsKey("storedFilePath").ShouldBeFalse();
            listBody.ShouldNotContain("storedFilePath", Case.Insensitive);
            listBody.ShouldNotContain(factory.AttachmentRoot.Replace("\\", "\\\\"));
        }

        [Fact]
        public async Task ElevenFiles_AreRejectedWith400_AndNothingIsStored()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("uploader");
            var eventId = await CreateEventAsync(client);
            var files = Enumerable.Range(1, AttachmentLimits.MaxFilesPerUpload + 1).Select(i => ($"f{i}.txt", new byte[] { 1 })).ToArray();

            using var response = await client.PostAsync($"/api/Calendar/{eventId}/attachments", Files(files));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            body["errors"]!["Files"]![0]!.GetValue<string>().ShouldContain("at most 10 files");
            Directory.Exists(Path.Combine(factory.AttachmentRoot, eventId.ToString())).ShouldBeFalse();
        }

        [Fact]
        public async Task AttachmentStoredOutsideTheRoot_Returns404()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("uploader");
            var eventId = await CreateEventAsync(client);

            // Like a row from before the root was configurable: an absolute path to a real file elsewhere.
            var outsideDir = Path.Combine(Path.GetTempPath(), "clefcraft-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(outsideDir);
            var outsideFile = Path.Combine(outsideDir, "old.pdf");
            await File.WriteAllTextAsync(outsideFile, "old");

            int id;
            using (var scope = factory.Services.CreateScope())
            {
                // SaveChanges writes an activity log entry attributed to the current user.
                scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "test-seeder") }))
                };
                var db = scope.ServiceProvider.GetRequiredService<ClefCraftDatabaseContext>();
                var row = new CalendarEventAttachment
                {
                    CalendarEventId = eventId,
                    FileName = "old.pdf",
                    StoredFilePath = outsideFile,
                    FileSize = 3,
                    ContentType = "application/pdf",
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = "someone"
                };
                db.Add(row);
                await db.SaveChangesAsync();
                id = row.Id;
            }

            try
            {
                using var download = await client.GetAsync($"/api/Calendar/attachments/download/{id}");
                download.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }
            finally
            {
                Directory.Delete(outsideDir, recursive: true);
            }
        }

        [Fact]
        public async Task AnotherUser_CannotDownload()
        {
            using var factory = new ClefCraftApiFactory();
            using var owner = await factory.CreateSignedInClientAsync("ownerone");
            using var stranger = await factory.CreateSignedInClientAsync("strangerone");
            var eventId = await CreateEventAsync(owner);
            var id = (await UploadAsync(owner, eventId, ("a.txt", new byte[] { 65 }))).Single()!["id"]!.GetValue<int>();

            using var download = await stranger.GetAsync($"/api/Calendar/attachments/download/{id}");

            download.StatusCode.ShouldBeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        }

        [Fact]
        public void UploadEndpoint_AcceptsBodiesUpToTheUploadLimit()
        {
            // Kestrel's default (30 MB) would refuse allowed uploads with 413 before the validator runs;
            // the endpoint must lift both the body and the multipart limits to just above 500 MB.
            var action = typeof(ClefCraft.API.Controllers.CalendarController).GetMethod("UploadAttachment")!;

            action.GetCustomAttribute<RequestSizeLimitAttribute>().ShouldNotBeNull();
            var sizeLimit = (long)typeof(RequestSizeLimitAttribute)
                .GetField("_bytes", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(action.GetCustomAttribute<RequestSizeLimitAttribute>())!;
            sizeLimit.ShouldBe(AttachmentLimits.MaxRequestBodyBytes);
            action.GetCustomAttribute<RequestFormLimitsAttribute>()!.MultipartBodyLengthLimit
                .ShouldBe(AttachmentLimits.MaxRequestBodyBytes);
            AttachmentLimits.MaxRequestBodyBytes.ShouldBeGreaterThan(AttachmentLimits.MaxUploadSizeBytes);
        }
    }
}
