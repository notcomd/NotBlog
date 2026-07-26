using Video.Domain.Entities;
using Video.Domain.ValueObjects;
using Video.Domain.SeedWork;
namespace Video.Domain.IRepository;

public interface IVideoRepository:IRepository<Videos>
{
    /// <summary>
    /// 查找所有视频
    /// </summary>
    /// <returns></returns>
    public Task<List<Videos>> FindByVideoListAsync();

    /// <summary>
    /// 查找视频
    /// </summary>
    /// <param name="findVideoGuid">视频GUID</param>
    /// <returns></returns>
    public Task<Videos> FindByVideoAsync(Guid findVideoGuid);

    /// <summary>
    /// 查找视频
    /// </summary>
    /// <param name="nvId">视频ID</param>
    /// <returns></returns>
    public Task<Videos> FindByNvidAsync(string nvId);

    /// <summary>
    /// 查找视频
    /// </summary>
    /// <param name="blurredVideoName">模糊视频名称</param>
    /// <returns></returns>
    public Task<Videos> FindByVideoAsync(string blurredVideoName);

    /// <summary>
    /// 分页查找视频
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns></returns>
    public Task<List<Videos>> PageByVideoAsync(int page, int pageSize);

    /// <summary>
    /// 查找视频
    /// </summary>
    /// <param name="name">视频名称</param>
    /// <returns></returns>
    public Task<Videos> FindByVideoName(string name);

    /// <summary>
    /// 查找视频
    /// </summary>
    /// <param name="videoName">视频名称</param>
    /// <returns></returns>
    public Task<List<Videos>> BlurredByVideoName(string videoName);

    /// <summary>
    /// 添加视频
    /// </summary>
    /// <param name="addVideo">视频</param>
    public Task AddByVideoAsync(Videos addVideo);

    /// <summary>
    /// 添加视频
    /// </summary>
    /// <param name="addVideos">视频列表</param>
    public Task AddByVideoRangeAsync(List<Videos> addVideos);

    /// <summary>
    /// 更新视频
    /// </summary>
    /// <param name="videoQuote">视频报价</param>
    public Task UpdateByQuoteAsync(VideoQuote videoQuote);

    /// <summary>
    /// 更新视频
    /// </summary>
    /// <param name="videoCollection">视频控制</param>
    public Task UpdateByControlAsync(VideoControl videoCollection);

    /// <summary>
    /// 更新视频
    /// </summary>
    /// <param name="timeSpace">时间空间</param>
    public Task UpdateByTimeSpaceAsync(TimeSpace timeSpace);

    public Task UpdateByVideoAsync(Videos videos);

    public Task DeleteByVideoControlAsync(VideoControl videoControl);

    public Task DeleteByVideoControlRangeAsync(List<VideoControl> videoControl);

    public Task InDeleteByVideoAsync(Videos videoControl);

    public Task InDeleteByVideoRangeAsync(List<Videos> videosList);

    // ── Standard Delete Operations ──

    /// <summary>Hard-delete a video by its GUID.</summary>
    public Task InDeleteByIdAsync(Guid videoGuid);
}