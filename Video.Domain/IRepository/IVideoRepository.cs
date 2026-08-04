using Video.Domain.Entities;
using Video.Domain.ValueObjects;
using Commons.SeedWork;
namespace Video.Domain.IRepository;

public interface IVideoRepository:IRepository<Videos, IUnitOfWork>
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
    /// 按主键加载完整视频（含评论、弹幕集合），供写路径使用，确保实体被 DbContext 跟踪。
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <returns></returns>
    public Task<Videos> FindByVideoWithDetailsAsync(Guid videoGuid);

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
    /// 更新视频互动计数（按视频主键过滤，防止全表覆盖）
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="videoQuote">视频报价</param>
    public Task UpdateByQuoteAsync(Guid videoGuid, VideoQuote videoQuote);

    /// <summary>
    /// 更新视频控制（按视频主键过滤）
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="videoControl">视频控制</param>
    public Task UpdateByControlAsync(Guid videoGuid, VideoControl videoControl);

    /// <summary>
    /// 更新时间空间（按视频主键过滤）
    /// </summary>
    /// <param name="videoGuid">视频GUID</param>
    /// <param name="timeSpace">时间空间</param>
    public Task UpdateByTimeSpaceAsync(Guid videoGuid, TimeSpace timeSpace);

    public Task UpdateByVideoAsync(Videos videos);

    /// <summary>
    /// 软删除单个视频（按视频主键过滤）
    /// </summary>
    public Task DeleteByVideoControlAsync(Guid videoGuid, VideoControl videoControl);

    /// <summary>
    /// 软删除多个视频（按各自视频主键过滤）
    /// </summary>
    public Task DeleteByVideoControlRangeAsync(List<Videos> videosList);

    public Task InDeleteByVideoAsync(Videos videoControl);

    public Task InDeleteByVideoRangeAsync(List<Videos> videosList);

    // ── Standard Delete Operations ──

    /// <summary>Hard-delete a video by its GUID.</summary>
    public Task InDeleteByIdAsync(Guid videoGuid);
}