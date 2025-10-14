

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FileDev.Domain.DomainEntities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructres.DbContext;
using DomainCommon;

namespace FileDev.Infrastructres.Repository;

public class FileGroupRepository : IFileGroupRepository
{
    private readonly FileDbContext _context;
    private readonly ILogger<FileGroupRepository> _logger;

    public FileGroupRepository(FileDbContext context, ILogger<FileGroupRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IUnitOfWork UnitOfWork => _context;

    /// <summary>
    /// 根据ID查询FileGroup
    /// </summary>
    public async ValueTask<FileGroup> QueryFileGroupByIdAsync(Guid fileGroupId)
    {
        try
        {
            var fileGroup = await _context.FileGroup
                .Include(fg => fg.Files)
                .Include(fg => fg.ChildFileGroups)
                .FirstOrDefaultAsync(fg => fg.Id == fileGroupId);

            if (fileGroup is null)
            {
                _logger.LogWarning("FileGroup with ID {FileGroupId} not found", fileGroupId);
                throw new KeyNotFoundException($"FileGroup with ID {fileGroupId} not found");
            }

            return fileGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying FileGroup by ID {FileGroupId}", fileGroupId);
            throw;
        }
    }

    /// <summary>
    ///   根据名称查询FileGroup
    /// </summary>
    /// <param name="fileGroupName">文件组名称</param>
    /// <returns>文件组实体</returns>
    /// <exception cref="KeyNotFoundException">文件组不存在</exception>
    public async ValueTask<FileGroup> QueryFileGroupByNameAsync(string fileGroupName)
    {
        try
        {
            var fileGroup = await _context.FileGroup
                .FirstOrDefaultAsync(fg => fg.FileGroupName == fileGroupName);

            if (fileGroup is null)
            {
                _logger.LogWarning("FileGroup with name {FileGroupName} not found", fileGroupName);
                throw new KeyNotFoundException($"FileGroup with name {fileGroupName} not found");
            }

            return fileGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying FileGroup by name {FileGroupName}", fileGroupName);
            throw;
        }
    }

    /// <summary>
    /// 创建新的FileGroup
    /// </summary>
    /// <param name="fileGroup">文件组实体</param>
    /// <returns>创建后的文件组实体</returns>
    /// <exception cref="ArgumentNullException">文件组实体为null</exception>
    /// <exception cref="InvalidOperationException">文件组已存在</exception>
    public async ValueTask<FileGroup> CreateFileGroupAsync(FileGroup fileGroup)
    {
        try
        {
            if (fileGroup is null)
            {
                _logger.LogError("FileGroup is null");
                throw new ArgumentNullException(nameof(fileGroup));
            }

            // 检查文件组是否已存在
            if (await _context.FileGroup.AnyAsync(fg => fg.FileGroupName == fileGroup.FileGroupName))
            {
                _logger.LogError($"FileGroup {fileGroup.FileGroupName} already exists");
                throw new InvalidOperationException($"FileGroup {fileGroup.FileGroupName} already exists");
            }

            await _context.FileGroup.AddAsync(fileGroup);
            _logger.LogInformation("Created new FileGroup: {FileGroupName} for user {UserGuid}", 
                fileGroup.FileGroupName, fileGroup.UserGuid);

            return fileGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating FileGroup");
            throw;
        }
    }

    /// <summary>
    /// 删除指定ID的FileGroup
    /// </summary>
    /// <param name="fileGroupId">文件组ID</param>
    /// <returns>删除操作是否成功</returns>
    /// <exception cref="KeyNotFoundException">文件组不存在</exception>
    /// <exception cref="InvalidOperationException">文件组包含文件或子文件组</exception>
    public async ValueTask<bool> DeleteFileGroupByIdAsync(Guid fileGroupId)
    {
        try
        {
            var fileGroup = await _context.FileGroup
                .FirstOrDefaultAsync(fg => fg.Id == fileGroupId);

            if (fileGroup is null)
            {
                _logger.LogWarning("FileGroup with ID {FileGroupId} not found for deletion", fileGroupId);
                return false;
            }
            // 软删除
            fileGroup.SoftDelete();
            _context.FileGroup.Update(fileGroup);            
            _logger.LogInformation("Soft deleted FileGroup: {FileGroupId}", fileGroupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting FileGroup with ID {FileGroupId}", fileGroupId);
            throw;
        }
    }

    /// <summary>
    /// 查询所有FileGroup
    /// </summary>
    public async ValueTask<List<FileGroup>> QueryAllFileGroupsAsync()
    {
        try
        {
            return await _context.FileGroup
                .Include(fg => fg.Files)
                .Include(fg => fg.ChildFileGroups)
                .Where(fg => !fg.IsDeleted)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying all FileGroups");
            throw;
        }
    }

    /// <summary>
    /// 根据ID更新FileGroup信息，使用指定的更新操作
    /// </summary>
    public async ValueTask UpdateFileGroupAsync(Guid fileGroupId, Func<FileGroup, Task> updateAction)
    {
        try
        {
            if (updateAction is null)
            {
                throw new ArgumentNullException(nameof(updateAction));
            }

            var fileGroup = await _context.FileGroup
                .FirstOrDefaultAsync(fg => fg.Id == fileGroupId);

            if (fileGroup is null)
            {
                _logger.LogWarning("FileGroup with ID {FileGroupId} not found for update", fileGroupId);
                throw new KeyNotFoundException($"FileGroup with ID {fileGroupId} not found");
            }

            await updateAction(fileGroup);
            _context.FileGroup.Update(fileGroup);
            
            _logger.LogInformation("Updated FileGroup: {FileGroupId}", fileGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating FileGroup with ID {FileGroupId}", fileGroupId);
            throw;
        }
    }

    /// <summary>
    /// 直接使用完整实体更新FileGroup信息
    /// </summary>
    public async ValueTask<bool> UpdateFileGroupAsync(FileGroup fileGroup)
    {
        try
        {
            if (fileGroup is null)
            {
                throw new ArgumentNullException(nameof(fileGroup));
            }

            var existingFileGroup = await _context.FileGroup
                .FirstOrDefaultAsync(fg => fg.Id == fileGroup.Id);

            if (existingFileGroup is null)
            {
                _logger.LogWarning("FileGroup with ID {FileGroupId} not found for update", fileGroup.Id);
                return false;
            }

            // 更新属性
            _context.Entry(existingFileGroup).CurrentValues.SetValues(fileGroup);
            _logger.LogInformation("Updated FileGroup: {FileGroupId}", fileGroup.Id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating FileGroup");
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID查询FileGroup列表
    /// </summary>
    public async ValueTask<List<FileGroup>> QueryFileGroupsByUserIdAsync(Guid userId)
    {
        try
        {
            return await _context.FileGroup
                .Include(fg => fg.Files)
                .Include(fg => fg.ChildFileGroups)
                .Where(fg => fg.UserGuid == userId && !fg.IsDeleted)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying FileGroups for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 根据名称查询FileGroup
    /// </summary>
    public async ValueTask<FileGroup?> QueryFileGroupByNameAsync(string name, Guid userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return await _context.FileGroup
                .Include(fg => fg.Files)
                .Include(fg => fg.ChildFileGroups)
                .FirstOrDefaultAsync(fg => 
                    fg.FileGroupName.ToLower() == name.ToLower() && 
                    fg.UserGuid == userId && 
                    !fg.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying FileGroup by name {Name} for user {UserId}", name, userId);
            throw;
        }
    }

    /// <summary>
    /// 检查FileGroup是否存在
    /// </summary>
    public async ValueTask<bool> ExistsAsync(string name, Guid userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return await _context.FileGroup
                .AnyAsync(fg => 
                    fg.FileGroupName.ToLower() == name.ToLower() && 
                    fg.UserGuid == userId && 
                    !fg.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if FileGroup exists with name {Name} for user {UserId}", name, userId);
            throw;
        }
    }
}
