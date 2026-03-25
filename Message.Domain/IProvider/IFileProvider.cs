using Message.Domain.Entities;

namespace Message.Domain.IProvider;

public interface IFileProvider
{
    Task<FileAttachment> UploadFileAsync(Guid messageId, string fileName, string fileType, long fileSize, Uri fileUri);
    Task<FileAttachment?> GetFileAsync(Guid attachmentId);
    Task<IEnumerable<FileAttachment>> GetMessageFilesAsync(Guid messageId);
    Task<IEnumerable<FileAttachment>> GetFilesByTypeAsync(string fileType);

    Task SetThumbnailAsync(Guid attachmentId, Uri thumbnailUri);
    Task UpdateDescriptionAsync(Guid attachmentId, string description);
    Task RecordDownloadAsync(Guid attachmentId);
    Task DeleteFileAsync(Guid attachmentId);

    Task<bool> FileExistsAsync(Guid attachmentId);
    Task<int> GetDownloadCountAsync(Guid attachmentId);
    Task<long> GetTotalFileSizeByMessageAsync(Guid messageId);

    bool IsImage(string fileType);
    bool IsVideo(string fileType);
    bool IsAudio(string fileType);
    bool IsDocument(string fileType);
    string GetFormattedFileSize(long fileSize);
}