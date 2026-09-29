using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Text;
using AttachmentStorage = ClefCraft.Infrastructure.FileAttachmentService;

namespace ClefCraft.Api.IntegrationTests.Attachments
{
    // Real disk I/O against a throwaway root, including the containment rule that keeps every
    // read/delete inside it.
    public sealed class FileAttachmentServiceTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "clefcraft-tests", Guid.NewGuid().ToString());
        private readonly string _outside = Path.Combine(Path.GetTempPath(), "clefcraft-tests", Guid.NewGuid().ToString());
        private readonly AttachmentStorage.FileAttachmentService _service;

        public FileAttachmentServiceTests()
        {
            _service = new AttachmentStorage.FileAttachmentService(
                Options.Create(new AttachmentStorage.AttachmentStorageOptions { RootPath = _root }));
        }

        public void Dispose()
        {
            foreach (var dir in new[] { _root, _outside })
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }

        private static IFormFile FormFile(string name, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", name) { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        }

        private static async Task<string> ReadAll(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        [Fact]
        public async Task Save_StoresARelativePathUnderTheRoot_AndReadsBack()
        {
            var saved = await _service.SaveAttachmentAsync(42, FormFile("Scales.PDF", "practice"), "user-1");

            saved.StoredFilePath.ShouldStartWith("42/");
            saved.StoredFilePath.ShouldEndWith(".PDF");
            Path.IsPathRooted(saved.StoredFilePath).ShouldBeFalse();
            File.Exists(Path.Combine(_root, saved.StoredFilePath)).ShouldBeTrue();

            await using var stream = await _service.OpenReadAsync(saved.StoredFilePath);
            (await ReadAll(stream!)).ShouldBe("practice");
        }

        [Fact]
        public async Task Save_DropsAnUnsafeExtension()
        {
            var saved = await _service.SaveAttachmentAsync(1, FormFile("notes.t xt/../x", "a"), "user-1");

            Path.GetExtension(saved.StoredFilePath).ShouldBeEmpty();
            saved.FileName.ShouldBe("notes.t xt/../x"); // the original name is only ever metadata
        }

        [Fact]
        public async Task PathsOutsideTheRoot_AreNeitherReadNorDeleted()
        {
            Directory.CreateDirectory(_outside);
            var outsideFile = Path.Combine(_outside, "secret.txt");
            await File.WriteAllTextAsync(outsideFile, "do not touch");

            // An absolute path (like the rows stored before the root was configurable) and a
            // relative path that climbs out of the root.
            var escape = Path.GetRelativePath(_root, outsideFile);
            escape.ShouldStartWith("..");

            foreach (var stored in new[] { outsideFile, escape })
            {
                (await _service.OpenReadAsync(stored)).ShouldBeNull();
                await _service.DeleteAttachmentFileAsync(stored);
            }

            File.Exists(outsideFile).ShouldBeTrue();
        }

        [Fact]
        public async Task Delete_RemovesAStoredFile_AndIgnoresAMissingOne()
        {
            var saved = await _service.SaveAttachmentAsync(3, FormFile("a.txt", "x"), "user-1");

            await _service.DeleteAttachmentFileAsync(saved.StoredFilePath);
            await _service.DeleteAttachmentFileAsync(saved.StoredFilePath);

            (await _service.OpenReadAsync(saved.StoredFilePath)).ShouldBeNull();
        }
    }
}
