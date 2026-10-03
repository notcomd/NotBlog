
namespace Video.Domain.IRepository;

/// <summary>
/// 视频收藏夹仓储接口 — 收藏夹聚合的读写入口（查询、分页、互动计数更新与软/硬删除）。
/// </summary>
public interface IVideoCollectionRepository : IRepository<VideoCollection, IUnitOfWork>
{
    /// <summary>
    /// 按 GUID 查询收藏夹
    /// </summary>
    /// <param name="findVideoCollectionGuid">收藏夹 GUID</param>
    /// <returns>收藏夹实体</returns>
    public Task<VideoCollection> FindByVideoCollectionAsync(Guid findVideoCollectionGuid);

    /// <summary>
    ///     查询videoCollection数据
    /// </summary>
    /// <param name="findVideoCollectionName">videocollectionName</param>
    /// <returns>返回一个videocollection</returns>
    public Task<VideoCollection> FindByVideoCollectionAsync(string findVideoCollectionName);

    /// <summary>
    ///     模糊查询
    /// </summary>
    /// <param name="blurredVideoCollection">要查询的合集名</param>
    /// <returns>返回有相关的明的合集类型为列表的VideoCollection</returns>
    public Task<List<VideoCollection>> BlurredByVideoCollectionAsync(string blurredVideoCollection);

    /// <summary>
    ///     分页查询
    /// </summary>
    /// <param name="page">页数</param>
    /// <param name="pageSize">页数数据量</param>
    /// <returns>返回VideoCollection的列表</returns>
    public Task<List<VideoCollection>> PageByVideoCollectionAsync(int page, int pageSize);

    /// <summary>
    ///     全部查询
    /// </summary>
    /// <returns></returns>
    public Task<List<VideoCollection>> FindByVideoCollectionListAsync();

    /// <summary>
    /// 新增收藏夹
    /// </summary>
    /// <param name="addVideoCollection">待新增的收藏夹</param>
    /// <returns></returns>
    public Task AddByVideoCollectionAsync(VideoCollection addVideoCollection);

    /// <summary>
    /// 更新收藏夹
    /// </summary>
    /// <param name="updateVideoCollection">待更新的收藏夹</param>
    /// <returns></returns>
    public Task UpdateByVideoCollectionAsync(VideoCollection updateVideoCollection);

    /// <summary>
    /// 批量更新收藏夹
    /// </summary>
    /// <param name="updateVideoCollections">待更新的收藏夹列表</param>
    public Task UpdateRangeByVideoCollectionAsync(List<VideoCollection> updateVideoCollections);

    /// <summary>
    /// 更新收藏夹互动计数（按收藏夹主键过滤，防止全表覆盖）
    /// </summary>
    public Task UpdateByQuoteAsync(Guid videoCollectionGuid, VideoQuote videoQuote);

    // ── Standard Delete Operations ──

    /// <summary>Soft-delete a collection by its GUID.</summary>
    public Task DeleteByIdAsync(Guid id);

    /// <summary>Hard-delete (physical removal) a collection by its GUID.</summary>
    public Task InDeleteByIdAsync(Guid id);
}