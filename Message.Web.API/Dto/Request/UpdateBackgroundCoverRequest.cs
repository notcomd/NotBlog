namespace Message.Web.API.Dto.Request;

/// <summary>更新背景封面请求（空白视为清除）。</summary>
public class UpdateBackgroundCoverRequest
{
    public Uri? BackgroundCoverUrl { get; init; }
}
