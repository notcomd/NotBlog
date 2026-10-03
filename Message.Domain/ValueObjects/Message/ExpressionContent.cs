namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 表情消息内容值对象。
/// 不可变，承载表情代码的原子约束（非空、长度上限），不可变。
/// </summary>
public class ExpressionContent : MessageContent
{
    private ExpressionContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("表情代码不能为空");
        if (value.Length > 100)
            throw new ArgumentException("表情代码不能超过100个字符");

        Value = value;
    }

    /// <summary>表情代码（非空，长度不超过 100 字符）</summary>
    public string Value { get; }

    /// <summary>内容业务类型，恒为表情消息。</summary>
    public override MessageType MessageType => MessageType.MessageExpression;

    /// <summary>创建表情内容值对象（校验非空与长度上限）。</summary>
    public static ExpressionContent Create(string value) => new(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    /// <summary>生成会话侧栏摘要，固定返回「[表情]」。</summary>
    public override string ToSessionSummary() => "[表情]";
}