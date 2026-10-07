using System.Security.Cryptography;
using Backend.Validation;
using Microsoft.Extensions.Configuration;

namespace Backend.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        // 1. 从配置读取路径
        var uploadRoot = config["Storage:UploadRoot"] ?? "uploads";

        // 2. 处理相对路径
        if (!Path.IsPathFullyQualified(uploadRoot))
        {
            uploadRoot = Path.Combine(env.ContentRootPath, uploadRoot);
        }

        // 3. 自动创建目录
        if (!Directory.Exists(uploadRoot))
        {
            Directory.CreateDirectory(uploadRoot);
            Console.WriteLine("[Storage] Upload directory created at: " + uploadRoot);
        }

        _root = Path.GetFullPath(uploadRoot);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folder)
    {
        var safeName = ImageUploadPolicy.GetSafeFileName(fileName);
        var folderPath = ResolveUnderRoot(folder);
        Directory.CreateDirectory(folderPath);

        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var filePath = ResolveUnderRoot(folder, uniqueName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(stream);

        return $"/uploads/{folder}/{uniqueName}";
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        var fullPath = ResolveStoredPath(filePath);

        if (!File.Exists(fullPath))
            return false;

        try
        {
            await Task.Run(() => File.Delete(fullPath));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<Stream?> OpenReadAsync(string filePath)
    {
        var fullPath = ResolveStoredPath(filePath);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = ResolveStoredPath(filePath);

        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<string> ComputeFileHashAsync(Stream fileStream)
    {
        using var md5 = MD5.Create();
        var hash = await md5.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private string ResolveStoredPath(string filePath)
    {
        const string prefix = "/uploads/";
        if (string.IsNullOrWhiteSpace(filePath) ||
            !filePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException("文件路径无效");
        }

        var relative = filePath[prefix.Length..]
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        return ResolveUnderRoot(relative);
    }

    private string ResolveUnderRoot(params string[] segments)
    {
        if (segments.Length == 0 || segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            throw new InvalidDataException("文件路径无效");
        }

        var candidate = Path.GetFullPath(Path.Combine(new[] { _root }.Concat(segments).ToArray()));
        var rootPrefix = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!candidate.StartsWith(rootPrefix, pathComparison))
        {
            throw new InvalidDataException("文件路径超出存储目录");
        }

        return candidate;
    }
}
