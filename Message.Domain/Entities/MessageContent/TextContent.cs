using Message.Domain.SeedWork;

namespace Message.Domain.Entities.MessageContent;

public class TextContent : ValueObject
{
    private TextContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("文本内容不能为空");
        if (value.Length > 2000)
            throw new ArgumentException("文本内容不能超过2000个字符");

        Value = value;
    }

    public string Value { get; }

    public static TextContent Create(string value) => new(value);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}