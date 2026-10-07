namespace Backend.Validation;

public static class ImageUploadPolicy
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
    };

    public static void Validate(string fileName, long fileLength, long maxFileSizeBytes)
    {
        if (fileLength <= 0)
        {
            throw new InvalidDataException("图片文件为空");
        }

        if (fileLength > maxFileSizeBytes)
        {
            throw new InvalidDataException($"单个图片不能超过 {maxFileSizeBytes / 1024 / 1024} MB");
        }

        var safeName = GetSafeFileName(fileName);
        if (!AllowedExtensions.Contains(Path.GetExtension(safeName)))
        {
            throw new InvalidDataException("仅支持 JPG、PNG、GIF、BMP 和 WebP 图片");
        }
    }

    public static string GetSafeFileName(string fileName)
    {
        var normalized = (fileName ?? string.Empty).Replace('\\', '/');
        var safeName = Path.GetFileName(normalized);

        if (string.IsNullOrWhiteSpace(safeName) ||
            safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidDataException("文件名无效");
        }

        return safeName;
    }
}
