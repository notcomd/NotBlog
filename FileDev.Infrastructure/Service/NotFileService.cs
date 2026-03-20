using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructure.Service;

public class NotFileService(INotFileRepository notFileRepository, ILogger<NotFileService> logger) : INotFileService
{
    public async Task CreateFileAsync(Guid userId, string fileName, HashSet<string>? fileTags, string? fileDescription,
        FileType fileType,
        double fileSize, Uri fileUri, string fileMd5, FileIdentity fileIdentity = FileIdentity.FilePrivate)
    {
        var file = new NotFile.NotFileBuilder()
            .WithFileName(fileName)
            .WithFileTags(fileTags ?? [])
            .WithFileDescription(fileDescription??string.Empty)
            .WithFileType(fileType)
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
        var fileData=await notFileRepository.GetFilesByUserIdAsync(userId);
        return fileData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFile?> GetFileByIdAsync(Guid fileId)
    {
        var fileData=await notFileRepository.GetFileByIdAsync(fileId);
        if (fileData.IsDeleted)
        {
            logger.LogError("File not found {FileId}", fileId);
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
            logger.LogError("File not found {FileId}", fileId);
            return;
        }
        file.UpdateFileData(fileName, fileTags, fileDescription, fileIdentity, fileMd5);
        await notFileRepository.UpdateFileAsync(file);
    }

    public async Task DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await GetFileByIdAsync(fileId);
        if (file is null)
        {
            logger.LogError("File not found {FileId}", fileId);
            return;
        }

        if (file.UserId != userId)
        {
            logger.LogError("User not authorized to delete file {FileId}", fileId);
            throw new UnauthorizedAccessException();
        }
        file.SoftDelete();
        await notFileRepository.UpdateFileAsync(file);
        logger.LogInformation("File deleted {FileId}", fileId);
    }
}