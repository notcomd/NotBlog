
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

    public string Url { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? Image { get; private set; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Url;
        yield return Title ?? string.Empty;
        yield return Description ?? string.Empty;
        yield return Image ?? string.Empty;
    }
}
