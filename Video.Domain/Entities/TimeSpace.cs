namespace Video.Domain.Entities;

public record TimeSpace(DateTimeOffset createAt, DateTimeOffset updateAt)
{
    public DateTimeOffset CreateAt { get; private set; }
    public DateTimeOffset UpdateAt { get; private set; }

    public void ResetCreateAt(DateTimeOffset createAts)
    {
        CreateAt = createAts;
    }

    public void ResetUpdateAt(DateTimeOffset updateAts)
    {
        UpdateAt = updateAts;
    }
}