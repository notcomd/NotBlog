
namespace Message.Domain.ValueObjects.Tweet;

/// <summary>
/// 链接元数据值对象（推文附带链接的预览信息）。
/// 不可变，作为 <see cref="Entities.Tweet.Tweet"/> 聚合的一部分被序列化存储。
/// </summary>
public class LinkMetadata : ValueObject
{
    private LinkMetadata()
    {
    }

    /// <summary>创建链接元数据值对象（URL 不能为空）。</summary>
    public static LinkMetadata Create(string url, string? title = null, string? description = null, string? image = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("链接URL不能为空", nameof(url));

        return new LinkMetadata
        {
            Url = url,
            Title = title,
            Description = description,
            Image = image
        };
    }

    /// <summary>链接 URL</summary>
    public string Url { get; private set; } = string.Empty;
    /// <summary>链接标题（可选）</summary>
    public string? Title { get; private set; }
    /// <summary>链接描述（可选）</summary>
    public string? Description { get; private set; }
    /// <summary>链接配图 URL（可选）</summary>
    public string? Image { get; private set; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Url;
        yield return Title ?? string.Empty;
        yield return Description ?? string.Empty;
        yield return Image ?? string.Empty;
    }
}
