using Backend.Services;
using Backend.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Backend.Tests;

public class ImageUploadSecurityTests
{
    [Fact]
    public void GetSafeFileName_RemovesClientPath()
    {
        var result = ImageUploadPolicy.GetSafeFileName("../../private/example.jpg");

        Assert.Equal("example.jpg", result);
    }

    [Theory]
    [InlineData("payload.exe", 1024)]
    [InlineData("empty.jpg", 0)]
    [InlineData("large.jpg", 2049)]
    public void Validate_RejectsUnsafeUpload(string fileName, long length)
    {
        Assert.Throws<InvalidDataException>(() =>
            ImageUploadPolicy.Validate(fileName, length, 2048));
    }

    [Fact]
    public async Task Storage_ConfinesFilesToConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"image-storage-test-{Guid.NewGuid():N}");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:UploadRoot"] = root
                })
                .Build();
            var environment = new TestWebHostEnvironment { ContentRootPath = root };
            var storage = new LocalFileStorageService(environment, configuration);

            await using var content = new MemoryStream([1, 2, 3]);
            var storedPath = await storage.SaveFileAsync(content, "../../example.jpg", "1");

            Assert.StartsWith("/uploads/1/", storedPath, StringComparison.Ordinal);
            Assert.DoesNotContain("..", storedPath, StringComparison.Ordinal);
            Assert.True(await storage.FileExistsAsync(storedPath));
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                storage.OpenReadAsync("/uploads/../../outside.jpg"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Backend.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
