using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructure.Service;

public class NotFileGroupService(INotFileGroupRepository notFileGroupRepository, ILogger<NotFileGroupService> logger)
    : INotFileGroupService
{
    public async Task CreateNotFileGroupAsync(Guid userId, string groupName, string? groupDescription,
        HashSet<string>? tags = null, FileType fileType = FileType.CompressFiles,
        FileIdentity fileIdentity = FileIdentity.FilePublic)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            logger.LogError("File group name is empty");
            return;
        }

        if (userId == Guid.Empty)
        {
            logger.LogError("User id is empty");
            return;
        }

        var group = await notFileGroupRepository.GetNotFileGroupByNameAsync(groupName);
        if (group != null)
        {
            logger.LogError("File group name already exists {GroupName}", groupName);
            return;
        }

        var data = new NotFileGroup.NotFileGroupBuilder()
            .WithUserId(userId)
            .WithFileGroupName(groupName)
            .WithFileGroupDescription(groupDescription ?? string.Empty)
            .WithFileGroupTags(tags ?? [])
            .WithFileIdentity(fileIdentity)
            .Build();
        logger.LogInformation("File group created successfully {GroupName}", groupName);
        await notFileGroupRepository.InsertNotFileGroupAsync(data);
    }

    public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        var listData = await notFileGroupRepository.GetNotFileGroupsByUserIdAsync(userId);
        return listData.Where(en => !en.IsDeleted);
    }

    public async Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId)
    {
        var data = await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
        return data;
    }

    public async Task AddFileToNotFileGroupAsync(Guid notFileGroupId, Guid fileId)
    {
        var groupData = await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
        if (groupData == null)
        {
            throw new ArgumentNullException(nameof(groupData), "File group not found");
        }

        if (!groupData.FileIds.Contains(fileId))
        {
            groupData.AddFile(fileId);
            logger.LogInformation("File added to file group successfully {FileId}", fileId);
        }
        else
        {
            logger.LogError("File already exists in file group {FileId}", fileId);
        }
    }

    public async Task RemoveFileFromNotFileGroupAsync(Guid notFileGroupId, Guid fileId)
    {
        var groupData = await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
        if (groupData.FileIds.Contains(fileId))
        {
            groupData.RemoveFile(fileId);
            logger.LogInformation("File removed from file group successfully {FileId}", fileId);
        }

        logger.LogError("File not found in file group {FileId}", fileId);
    }

    public async Task UpdateNotFileGroupAsync(Guid notFileGroupId, string groupName, string? groupDescription,
        HashSet<string>? tags = null)
    {
        var data = await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
        data.UpdateFileGroup(groupName, tags, groupDescription, FileIdentity.FilePublic, FileType.CompressFiles);
        await notFileGroupRepository.UpdateNotFileGroupAsync(data);
        logger.LogInformation("File group updated successfully {GroupName}", data.FileGroupName);
    }

    public async Task DeleteNotFileGroupAsync(Guid notFileGroupId)
    {
        var data = await notFileGroupRepository.GetNotFileGroupByIdAsync(notFileGroupId);
        data.SoftDelete();
        await notFileGroupRepository.UpdateNotFileGroupAsync(data);
        logger.LogInformation("File group deleted successfully {GroupName}", data.FileGroupName);
    }
}