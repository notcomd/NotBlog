namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 保存推文草稿命令。
/// <para>CQRS 命令侧：仅返回新草稿的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">作者用户 ID</param>
/// <param name="Content">草稿内容</param>
/// <param name="FileIds">FileDev 文件 ID 列表（草稿暂不落媒体，保留字段）</param>
/// <param name="LinkUrl">链接 URL</param>
/// <param name="Visibility">可见性</param>
public record SaveDraftCommand(
    Guid UserId,
    string Content,
    IEnumerable<Guid>? FileIds,
    string? LinkUrl,
    Visibility Visibility) : IRequest<Guid>;

