namespace Video.Domain.ValueObjects;

public record TimeSpace
{

    public TimeSpace()
    {
        CreateAt = DateTimeOffset.UtcNow;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreateAt { get; init; }
    public DateTimeOffset UpdateAt { get; private set; }

    public TimeSpace(DateTimeOffset createAt, DateTimeOffset updateAt)
    {
        CreateAt = createAt;
        UpdateAt = updateAt;
    }

    public void ResetUpdateAt(DateTimeOffset updateAt)
    {
        UpdateAt = updateAt;
    }
}