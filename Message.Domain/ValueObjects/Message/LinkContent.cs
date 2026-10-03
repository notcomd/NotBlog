
namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 链接消息内容值对象。
/// 不可变，封装链接 URL 与可选标题/描述。
/// </summary>
public class LinkContent : MessageContent
{
    private LinkContent(Uri url, string? title = null, string? description = null)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Title = title;
        Description = description;
    }

    /// <summary>链接 URL（非空）</summary>
    public Uri Url { get; }
    /// <summary>链接标题（可选）</summary>
    public string? Title { get; }
    /// <summary>链接描述（可选）</summary>
    public string? Description { get; }

    /// <summary>内容业务类型，恒为链接消息。</summary>
    public override MessageType MessageType => MessageType.MessageLink;

    /// <summary>创建链接内容值对象（URL 不能为空）。</summary>
    public static LinkContent Create(Uri url, string? title = null, string? description = null)
        => new(url, title, description);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Url;
        yield return Title ?? string.Empty;
        yield return Description ?? string.Empty;
    }

    /// <summary>生成会话侧栏摘要，优先取标题，否则取 URL。</summary>
    public override string ToSessionSummary() => Title ?? Url.ToString();
}
