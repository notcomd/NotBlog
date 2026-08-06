
namespace Message.Domain.ValueObjects;

public class MessageContent : ValueObject
{
    private const int MaxTextLength = 4000;
    private const int MaxExpressionLength = 100;

    private MessageContent(string content)
    {
        Content = content;
    }

    public string Content { get; }
    public int Length => Content.Length;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    public static MessageContent CreateText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("消息内容不能为空", nameof(content));

        if (content.Length > MaxTextLength)
            throw new ArgumentException($"消息内容长度不能超过 {MaxTextLength} 个字符", nameof(content));

        return new MessageContent(content.Trim());
    }

    public static MessageContent CreateExpression(string expressionCode)
    {
        if (string.IsNullOrWhiteSpace(expressionCode))
            throw new ArgumentException("表情代码不能为空", nameof(expressionCode));

        if (expressionCode.Length > MaxExpressionLength)
            throw new ArgumentException($"表情代码长度不能超过 {MaxExpressionLength} 个字符", nameof(expressionCode));

        return new MessageContent(expressionCode.Trim());
    }

    public static MessageContent Empty() => new(string.Empty);

    public bool Contains(string searchTerm) => Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

    public MessageContent Truncate(int maxLength)
    {
        if (Content.Length <= maxLength)
            return this;

        return new MessageContent(Content.Substring(0, maxLength) + "...");
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Content;
    }

    public override string ToString() => Content;

    public static implicit operator string(MessageContent content) => content.Content;
    public static explicit operator MessageContent(string content) => CreateText(content);
}