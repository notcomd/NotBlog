using Video.Domain.Entities;
using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.IRepository;

public interface IVideoCollectionRepository : IRepository<VideoCollection>
{
    /// <summary>
    /// </summary>
    /// <param name="findVideoCollectionGuid"></param>
    /// <returns></returns>
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
    /// </summary>
    /// <param name="addVideoCollection"></param>
    /// <returns></returns>
    public Task AddByVideoCollectionAsync(VideoCollection addVideoCollection);

    /// <summary>
    /// </summary>
    /// <param name="updataVideoCollection"></param>
    /// <returns></returns>
    public Task UpdateByVideoCollectionAsync(VideoCollection updateVideoCollection);

    public Task UpdateRangeByVideoCollectionAsync(List<VideoCollection> updateVideoCollections);

    public Task UpdateByQuoteAsync(VideoQuote videoQuote);

    // ── Standard Delete Operations ──

    /// <summary>Soft-delete a collection by its GUID.</summary>
    public Task DeleteByIdAsync(Guid id);

    /// <summary>Hard-delete (physical removal) a collection by its GUID.</summary>
    public Task InDeleteByIdAsync(Guid id);
}