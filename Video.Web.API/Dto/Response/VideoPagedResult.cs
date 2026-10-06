namespace Video.Web.API.Dto.Response;

/// <summary>视频分页结果。</summary>
/// <typeparam name="T">列表项类型</typeparam>
public class VideoPagedResult<T>
{
    /// <summary>当前页数据</summary>
    public List<T> Items { get; init; } = [];

    /// <summary>总条数</summary>
    public int TotalCount { get; init; }

    /// <summary>当前页码（从 1 开始）</summary>
    public int Page { get; init; }

    /// <summary>每页条数</summary>
    public int PageSize { get; init; }

    /// <summary>总页数</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
