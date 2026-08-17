namespace Video.Domain.ValueObjects;

public record TimeSpace
{
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