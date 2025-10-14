



using DomainCommon;

using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructures.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Markdown.Infrastructres.Repository;


public class BlockVersionRepository : IBlockVersionRepository
{

    private readonly MarkdownDbContext _dbContext;

    public IUnitOfWork UnitOfWork => _dbContext;

    private readonly ILogger<BlockVersionRepository> _logger;

    private readonly INotDateTime _notDateTimeProvider;


    public BlockVersionRepository(MarkdownDbContext dbContext, ILogger<BlockVersionRepository> logger, INotDateTime notDateTimeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notDateTimeProvider = notDateTimeProvider;
    }


    /// <summary>
    /// 添加MarkDownBlock实体的版本
    /// </summary>
    /// <param name="blockVersion">待添加的MarkDownBlock实体的版本</param>
    /// <returns>添加了ID的MarkDownBlock实体的版本对象</returns>
    public async Task<BlockVersion> AddBlockVersionAsync(BlockVersion blockVersion)
    {
        var data = await _dbContext.BlockVersion.AddAsync(blockVersion);
        if (data is null)
            throw new Exception("添加BlockVersion失败");
        return data.Entity;
    }

    public async Task<List<BlockVersion>> GetBlockVersionsByAllAsync()
    {
        var dataList = await _dbContext.BlockVersion.ToListAsync();
        return dataList;
    }

    public Task<List<BlockVersion>> GetBlockVersionsByMarkDownGroupIdAsync(Guid markDownGroupId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<BlockVersion>> GetBlockVersionsByMarkDownUserIdAsync(Guid markDownUserId)
    {
        var data = await _dbContext.BlockVersion
             .Where(en => en.UserId == markDownUserId)
             .Select(en => en)
             .ToListAsync();

        return data;
    }

    public Task<List<BlockVersion>> GetBlockVersionsByMarkDownBlockIdAsync(Guid markDownBlockId)
    {
        throw new NotImplementedException();
    }

    public Task<BlockVersion> GetLatestBlockVersionByMarkDownBlockIdAsync(Guid markDownBlockId)
    {
        throw new NotImplementedException();
    }

    public Task<BlockVersion> GetBlockVersionByMarkDownBlockIdAsync(Guid markDownBlockId, Guid blockVersionId)
    {
        throw new NotImplementedException();
    }


    public Task<BlockVersion> UpdateBlockVersionAsync(BlockVersion blockVersion)
    {
        throw new NotImplementedException();
    }

    public Task<BlockVersion> UpdateBlockVersionAsync(Guid blockVersionId, Func<BlockVersion, Task<BlockVersion>> updateAction)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteBlockVersionAsync(BlockVersion blockVersion)
    {
        throw new NotImplementedException();
    }

    IUnitOfWork IRepository<BlockVersion>.UnitOfWork => throw new NotImplementedException();
}