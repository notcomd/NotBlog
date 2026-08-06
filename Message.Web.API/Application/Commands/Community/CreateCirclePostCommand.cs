namespace Message.Web.API.Application.Commands.Community;

/// <summary>
/// 圈子发帖命令。
/// <para>
/// 复用 Tweet 聚合根（图文/视频/链接媒体体系），发布即 Approved（圈子内免审核）；
/// 帖子仅圈子成员可见、成员可互动；可关联话题（最多 10 个）。
/// </para>
/// </summary>
public record CreateCirclePostCommand(
    Guid UserId,
    Guid CircleGuid,
    string Content,
    IEnumerable<Guid>? FileIds,
    string? LinkUrl,
    IEnumerable<Guid>? TopicGuids) : IRequest<Guid>;
