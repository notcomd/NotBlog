namespace Message.Web.API.Dto.Request;

/// <summary>发布圈子帖（复用 Tweet 媒体体系：FileDev 文件 ID 列表）</summary>
public class CreateCirclePostRequest
{
    public Guid CircleGuid { get; init; }
    public string Content { get; init; } = string.Empty;
    /// <summary>FileDev 文件 ID 列表（图文/视频）</summary>
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    /// <summary>关联话题 ID 列表（最多 10 个）</summary>
    public List<Guid>? TopicGuids { get; init; }
}
