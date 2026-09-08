using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TaskBoard.Application.Security;

namespace TaskBoard.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;
    private readonly long _maxFileSizeBytes;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".pdf"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/webp", "application/pdf"
    };

    public long MaxFileSizeBytes => _maxFileSizeBytes;

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:UploadPath"];
        _uploadDirectory = !string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");

        _maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeMb", 10) * 1024 * 1024;

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public bool IsAllowedFileType(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return false;
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return false;
        }

        return true;
    }

    public async Task<(string storedFileName, string storagePath, long fileSize)> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedFileType(originalFileName, contentType))
        {
            throw new InvalidOperationException("Invalid or unsupported file type.");
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_uploadDirectory, storedFileName);

        // Prevent path traversal
        var fullPath = Path.GetFullPath(destinationPath);
        if (!fullPath.StartsWith(_uploadDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file destination path.");
        }

        await using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await fileStream.CopyToAsync(outputStream, cancellationToken);
        var fileSize = outputStream.Length;

        if (fileSize > _maxFileSizeBytes)
        {
            outputStream.Close();
            File.Delete(fullPath);
            throw new InvalidOperationException($"File size exceeds the allowed limit of {_maxFileSizeBytes / (1024 * 1024)}MB.");
        }

        return (storedFileName, fullPath, fileSize);
    }

    public Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        // Verify storagePath is within upload directory
        var fullPath = Path.GetFullPath(storagePath);
        if (!fullPath.StartsWith(_uploadDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || !File.Exists(storagePath))
        {
            return Task.FromResult(false);
        }

        var fullPath = Path.GetFullPath(storagePath);
        if (!fullPath.StartsWith(_uploadDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }
}
