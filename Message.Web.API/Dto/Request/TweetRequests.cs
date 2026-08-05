namespace Message.Web.API.Dto.Request;

public class CreateTweetRequest
{
    public string Content { get; init; } = string.Empty;

    /// <summary>FileDev 文件 ID 列表（媒体推文；来自上传接口返回的 FileRef.FileId）</summary>
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    public string? Visibility { get; init; }
}

public class UpdateTweetRequest
{
    public string Content { get; init; } = string.Empty;
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    public string? Visibility { get; init; }
}

public class CreateCommentRequest
{
    public Guid TweetGuid { get; init; }
    public string Content { get; init; } = string.Empty;
    public Guid? ParentGuid { get; init; }
    public Guid? ReplyToGuid { get; init; }
}

public class SubmitReportRequest
{
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetGuid { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public List<string>? EvidenceUrls { get; init; }
}

public class AuditActionRequest
{
    public string Reason { get; init; } = string.Empty;
}

public class ResolveReportRequest
{
    public string Action { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
}
