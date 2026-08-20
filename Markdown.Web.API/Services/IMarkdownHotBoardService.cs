namespace Markdown.Web.API.Services;

/// <summary>
///     Markdown 热点榜服务接口
/// </summary>
public interface IMarkdownHotBoardService
{
    /// <summary>
    ///     获取热点榜 TopN（Redis ZSet 直读；miss/故障时单飞重建或降级 DB 计算）
    /// </summary>
    Task<List<MarkdownHotItem>> GetHotBoardAsync(int take = 20, CancellationToken ct = default);

    /// <summary>
    ///     写侧钩子：单文档计数变更后重算热度分（DB HeatScore 列 + Redis ZSet 同步）
    /// </summary>
    Task UpdateScoreAsync(Guid markDownGuid, CancellationToken ct = default);

    /// <summary>
    ///     全量重建热点榜（定时任务调用，防重入）
    /// </summary>
    Task RebuildAsync(CancellationToken ct = default);
}
