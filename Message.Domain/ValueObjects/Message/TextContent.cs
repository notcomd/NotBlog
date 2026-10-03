
namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 文本消息内容值对象。
/// 承载文本消息的原子内容约束（非空、长度上限），不可变。
/// </summary>
public class TextContent : MessageContent
{
    private TextContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("文本内容不能为空");
        if (value.Length > 2000)
            throw new ArgumentException("文本内容不能超过2000个字符");

        Value = value;
    }

    /// <summary>文本内容（非空，长度不超过 2000 字符）</summary>
    public string Value { get; }

    /// <summary>内容业务类型，恒为文本消息。</summary>
    public override MessageType MessageType => MessageType.MessageText;

    /// <summary>创建文本内容值对象（校验非空与长度上限）。</summary>
    public static TextContent Create(string value) => new(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    /// <summary>生成会话侧栏摘要，直接返回文本内容。</summary>
    public override string ToSessionSummary() => Value;
}
