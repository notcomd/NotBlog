namespace Message.Web.API.Application.Commands.Tweets;
/// <summary>
/// 创建推文命令。
/// <para>CQRS 命令侧：仅返回新推文的标识（Guid），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="UserId">作者用户 ID</param>
/// <param name="Content">推文内容</param>
/// <param name="FileIds">FileDev 文件 ID 列表（媒体推文；服务端校验归属并解析元数据）</param>
/// <param name="LinkUrl">链接 URL</param>
/// <param name="Visibility">可见性</param>
/// <param name="AsDraft">
/// 是否存为草稿。默认 true（草稿）；为 false 时创建后立即提交审核（Draft → Pending），
/// 不直接置为已通过，仍须走管理端审核流程。
/// </param>
public record CreateTweetCommand(
    Guid UserId,
    string Content,
    IEnumerable<Guid>? FileIds,
    string? LinkUrl,
    Visibility Visibility,
    bool AsDraft = true) : IRequest<Guid>;

