namespace Message.Web.API.Dto.Request;
public class CreateTweetRequest
{
    public string Content { get; init; } = string.Empty;

    /// <summary>FileDev 文件 ID 列表（媒体推文；来自上传接口返回的 FileRef.FileId）</summary>
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    public string? Visibility { get; init; }
}

