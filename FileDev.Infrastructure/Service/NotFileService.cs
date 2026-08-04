using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructure.Service;

public class NotFileService(INotFileRepository notFileRepository, ILogger<INotFileService> logger) : INotFileService
{
    public async Task CreateFileAsync(Guid userId, string fileName, HashSet<string>? fileTags, string? fileDescription,
        FileType fileType,
        long fileSize, Uri fileUri, string fileMd5, FileIdentity fileIdentity = FileIdentity.FilePrivate)
    {
        var file = new NotFile.NotFileBuilder()
            .WithFileName(fileName)
            .WithFileTags(fileTags ?? [])
            .WithFileDescription(fileDescription ?? string.Empty)
            .WithFileSize(fileSize)
            .WithFileUri(fileUri)
            .WithFileMd5(fileMd5)
            .WithFileIdentity(fileIdentity)
            .WithUserId(userId)
            .Build();
        await notFileRepository.InsertFileAsync(file);
    }

    public async Task<IEnumerable<NotFile>> GetFilesByUserIdAsync(Guid userId)
    {
        var fileData = await notFileRepository.GetFilesByUserIdAsync(userId);
        return fileData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        var fileData = await notFileRepository.GetFileByIdAsync(fileId);
        if (fileData is { IsDeleted: true })
        {
            // 可预期的业务性失败（文件已被删除），用 Warning 而非 Error
            logger.LogWarning("File not found {FileId}", fileId);
            return null;
        }

        return fileData;
    }

    public async Task UpdateFileAsync(Guid fileId, string fileName, HashSet<string>? fileTags, string fileDescription,
        FileIdentity fileIdentity,
        string fileMd5)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            // 不再静默吞错：文件不存在属于可预期业务性失败，记录警告并抛异常交由上层统一处理
            logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileException($"文件不存在: {fileId}");
        }

        file.ChangeFileData(fileName, fileTags, fileDescription, fileIdentity, fileMd5);
        await notFileRepository.UpdateFileAsync(file);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public async Task DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            // 不再静默吞错：文件不存在属于可预期业务性失败，记录警告并抛异常交由上层统一处理
            logger.LogWarning("File not found {FileId}", fileId);
            throw new NotFileException($"文件不存在: {fileId}");
        }

        if (file.UserId != userId)
        {
            // 越权删除属于业务性拒绝，用 Warning 级别记录，并抛异常交由上层统一处理
            logger.LogWarning("User not authorized to delete file {FileId}",
                          fileId);
            throw new NotFileException($"无权删除文件: {fileId}");
        }

        file.SoftDelete();
        await notFileRepository.UpdateFileAsync(file);
        logger.LogInformation("File deleted {FileId}",
                              fileId);
    }
}