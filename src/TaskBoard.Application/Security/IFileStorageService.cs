namespace TaskBoard.Application.Security;

public interface IFileStorageService
{
    Task<(string storedFileName, string storagePath, long fileSize)> SaveFileAsync(
        Stream fileStream, 
        string originalFileName, 
        string contentType, 
        CancellationToken cancellationToken = default);

    Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
    bool IsAllowedFileType(string fileName, string contentType);
    long MaxFileSizeBytes { get; }
}
