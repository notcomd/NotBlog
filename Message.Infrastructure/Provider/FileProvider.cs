namespace Message.Infrastructure.Provider;

public class FileProvider : IFileProvider
{
    private readonly IFileAttachmentRepository _fileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FileProvider(
        IFileAttachmentRepository fileRepository,
        IUnitOfWork unitOfWork)
    {
        _fileRepository = fileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FileAttachment> UploadFileAsync(Guid messageId, string fileName, string fileType, long fileSize,
        Uri fileUri)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为空", nameof(fileName));

        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");

        var attachment = new FileAttachment(messageId, fileName, fileType, fileSize, fileUri);
        await _fileRepository.AddAsync(attachment);
        await _unitOfWork.SaveEntitiesAsync();

        return attachment;
    }

    public async Task<FileAttachment?> GetFileAsync(Guid attachmentId)
    {
        return await _fileRepository.GetByIdAsync(attachmentId);
    }

    public async Task<IEnumerable<FileAttachment>> GetMessageFilesAsync(Guid messageId)
    {
        return await _fileRepository.GetByMessageIdAsync(messageId);
    }

    public async Task<IEnumerable<FileAttachment>> GetFilesByTypeAsync(string fileType)
    {
        return await _fileRepository.GetByFileTypeAsync(fileType);
    }

    public async Task SetThumbnailAsync(Guid attachmentId, Uri thumbnailUri)
    {
        var file = await _fileRepository.GetByIdAsync(attachmentId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        file.SetThumbnail(thumbnailUri);
        await _fileRepository.UpdateAsync(file);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task UpdateDescriptionAsync(Guid attachmentId, string description)
    {
        var file = await _fileRepository.GetByIdAsync(attachmentId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        file.UpdateDescription(description);
        await _fileRepository.UpdateAsync(file);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task RecordDownloadAsync(Guid attachmentId)
    {
        var file = await _fileRepository.GetByIdAsync(attachmentId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        file.RecordDownload();
        await _fileRepository.UpdateAsync(file);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task DeleteFileAsync(Guid attachmentId)
    {
        var file = await _fileRepository.GetByIdAsync(attachmentId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        file.Delete();
        await _fileRepository.UpdateAsync(file);
        await _unitOfWork.SaveEntitiesAsync();
    }

    public async Task<bool> FileExistsAsync(Guid attachmentId)
    {
        return await _fileRepository.ExistsAsync(attachmentId);
    }

    public async Task<int> GetDownloadCountAsync(Guid attachmentId)
    {
        var file = await _fileRepository.GetByIdAsync(attachmentId);
        return file?.DownloadCount ?? 0;
    }

    public async Task<long> GetTotalFileSizeByMessageAsync(Guid messageId)
    {
        var files = await _fileRepository.GetByMessageIdAsync(messageId);
        return files.Sum(f => f.FileSize);
    }

    public bool IsImage(string fileType)
    {
        return fileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsVideo(string fileType)
    {
        return fileType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAudio(string fileType)
    {
        return fileType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDocument(string fileType)
    {
        var documentTypes = new[]
            { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument", "text/" };
        return documentTypes.Any(t => fileType.StartsWith(t, StringComparison.OrdinalIgnoreCase));
    }

    public string GetFormattedFileSize(long fileSize)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = fileSize;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}