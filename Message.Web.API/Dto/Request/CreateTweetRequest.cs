namespace Message.Web.API.Dto.Request;
public class CreateTweetRequest
{
    public string Content { get; init; } = string.Empty;

    /// <summary>FileDev 文件 ID 列表（媒体推文；来自上传接口返回的 FileRef.FileId）</summary>
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    public string? Visibility { get; init; }

    /// <summary>
    /// 是否存为草稿。可空：null 视为 true（草稿，向后兼容）；false 表示创建后立即提交审核。
    /// </summary>
    public bool? AsDraft { get; init; }
}

