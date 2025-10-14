

using DomainCommon;
using Markdown.Domain.Entities;

namespace Markdown.Domain.IRepository;


public interface IBlockVersionRepository : IRepository<BlockVersion>
{


    /// <summary>
    /// 获取所有MarkDownBlock实体的版本
    /// </summary>
    /// <returns>所有MarkDownBlock实体的版本</returns>
    Task<List<BlockVersion>> GetBlockVersionsByAllAsync();

    /// <summary>
    /// 根据MarkDownGroupId获取MarkDownGroup实体的MarkDown列表
    /// </summary>
    /// <param name="markDownGroupId">MarkDownGroup实体的ID</param>
    /// <returns>MarkDownGroup实体的MarkDown列表</returns>
    Task<List<BlockVersion>> GetBlockVersionsByMarkDownGroupIdAsync(Guid markDownGroupId);


    /// <summary>
    /// 根据MarkDownUserId获取MarkDownUser实体的MarkDown列表
    /// </summary>
    /// <param name="markDownUserId">MarkDownUser实体的ID</param>
    /// <returns>MarkDownUser实体的MarkDown列表</returns>
    Task<List<BlockVersion>> GetBlockVersionsByMarkDownUserIdAsync(Guid markDownUserId);


    /// <summary>
    /// 根据MarkDownBlockId获取MarkDownBlock实体的MarkDown列表
    /// </summary>
    /// <param name="markDownBlockId">MarkDownBlock实体的ID</param>
    /// <returns>MarkDownBlock实体的MarkDown列表</returns>
    Task<List<BlockVersion>> GetBlockVersionsByMarkDownBlockIdAsync(Guid markDownBlockId);

    /// <summary>
    /// 根据MarkDownBlockId获取MarkDownBlock实体的最新版本
    /// </summary>
    /// <param name="markDownBlockId">MarkDownBlock实体的ID</param>
    /// <returns>MarkDownBlock实体的最新版本</returns>
    Task<BlockVersion> GetLatestBlockVersionByMarkDownBlockIdAsync(Guid markDownBlockId);


    /// <summary>
    /// 根据MarkDownBlockId获取MarkDownBlock实体的指定版本
    /// </summary>
    /// <param name="markDownBlockId">MarkDownBlock实体的ID</param>
    /// <param name="blockVersionId">MarkDownBlock实体的版本ID</param>
    /// <returns>MarkDownBlock实体的指定版本</returns>
    Task<BlockVersion> GetBlockVersionByMarkDownBlockIdAsync(Guid markDownBlockId, Guid blockVersionId);


    /// <summary>
    /// 添加MarkDownBlock实体的版本
    /// </summary>
    /// <param name="blockVersion">待添加的MarkDownBlock实体的版本</param>
    /// <returns>添加了ID的MarkDownBlock实体的版本对象</returns>
    Task<BlockVersion> AddBlockVersionAsync(BlockVersion blockVersion);


    /// <summary>
    /// 更新MarkDownBlock实体的版本
    /// </summary>
    /// <param name="blockVersion">待更新的MarkDownBlock实体的版本</param>
    /// <returns>更新了的MarkDownBlock实体的版本对象</returns>
    Task<BlockVersion> UpdateBlockVersionAsync(BlockVersion blockVersion);


    /// <summary>
    /// 更新MarkDownBlock实体的版本
    /// </summary>
    /// <param name="blockVersionId">待更新的MarkDownBlock实体的版本ID</param>
    /// <param name="updateAction">更新操作</param>
    /// <returns>更新了的MarkDownBlock实体的版本对象</returns>
    Task<BlockVersion> UpdateBlockVersionAsync(Guid blockVersionId,Func<BlockVersion,Task<BlockVersion>> updateAction);


    /// <summary>
    /// 删除MarkDownBlock实体的版本
    /// </summary>
    /// <param name="blockVersion">待删除的MarkDownBlock实体的版本</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteBlockVersionAsync(BlockVersion blockVersion);





}