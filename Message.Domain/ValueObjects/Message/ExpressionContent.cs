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

    public string Value { get; }

    public override MessageType MessageType => MessageType.MessageExpression;

    public static ExpressionContent Create(string value) => new(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToSessionSummary() => "[表情]";
}