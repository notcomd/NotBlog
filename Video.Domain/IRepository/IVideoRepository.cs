namespace Video.Domain.IRepository;

/// <summary>
/// 视频仓储接口 — 视频聚合的读写入口（查询、分页、互动计数/控制更新、软删除与硬删除）。
/// </summary>
public interface IVideoRepository:IRepository<Videos, IUnitOfWork>
{
    /// <summary>
    /// 查找所有视频（按查看者身份收敛可见范围）。
    /// <para>
    /// 匿名/他人：仅返回「已审核通过 + 公开 + 未删除」的视频；
    /// 作者本人：可见自己的全部状态（未删除）；管理员：可见全部（未删除）。
    /// </para>
    /// </summary>
    /// <param name="viewerGuid">当前查看者 Guid（匿名传 null）</param>
    /// <param name="isAdmin">当前查看者是否为管理员</param>
    /// <returns></returns>
    public Task<List<Videos>> FindByVideoListAsync(Guid? viewerGuid, bool isAdmin);

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
    /// 按作者分页查询其视频（可按状态过滤；排除软删除；按创建时间倒序）。
    /// </summary>
    /// <param name="authorGuid">作者 Guid</param>
    /// <param name="status">状态过滤（null 表示全部状态）</param>
    /// <param name="page">页码（从 1 开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns></returns>
    public Task<List<Videos>> PageByAuthorAsync(Guid authorGuid, VideoStatus? status, int page, int pageSize);

    /// <summary>统计作者的视频总数（可按状态过滤；排除软删除）。</summary>
    /// <param name="authorGuid">作者 Guid</param>
    /// <param name="status">状态过滤（null 表示全部状态）</param>
    /// <returns></returns>
    public Task<int> CountByAuthorAsync(Guid authorGuid, VideoStatus? status);

    /// <summary>
    /// 管理端按状态分页查询视频（可按状态过滤；排除软删除；按创建时间倒序）。
    /// </summary>
    /// <param name="status">状态过滤（null 表示全部状态）</param>
    /// <param name="page">页码（从 1 开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns></returns>
    public Task<List<Videos>> PageByStatusAsync(VideoStatus? status, int page, int pageSize);

    /// <summary>统计视频总数（可按状态过滤；排除软删除）。</summary>
    /// <param name="status">状态过滤（null 表示全部状态）</param>
    /// <returns></returns>
    public Task<int> CountByStatusAsync(VideoStatus? status);

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

    /// <summary>
    /// 更新视频实体（按实体跟踪保存）
    /// </summary>
    /// <param name="videos">待更新的视频实体</param>
    public Task UpdateByVideoAsync(Videos videos);

    /// <summary>
    /// 软删除单个视频（按视频主键过滤）
    /// </summary>
    public Task DeleteByVideoControlAsync(Guid videoGuid, VideoControl videoControl);

    /// <summary>
    /// 软删除多个视频（按各自视频主键过滤）
    /// </summary>
    public Task DeleteByVideoControlRangeAsync(List<Videos> videosList);

    /// <summary>
    /// 硬删除单个视频（物理删除）
    /// </summary>
    /// <param name="videoControl">视频实体</param>
    public Task InDeleteByVideoAsync(Videos videoControl);

    /// <summary>
    /// 硬删除多个视频（物理删除）
    /// </summary>
    /// <param name="videosList">视频实体列表</param>
    public Task InDeleteByVideoRangeAsync(List<Videos> videosList);

    // ── Standard Delete Operations ──

    /// <summary>Hard-delete a video by its GUID.</summary>
    public Task InDeleteByIdAsync(Guid videoGuid);
}