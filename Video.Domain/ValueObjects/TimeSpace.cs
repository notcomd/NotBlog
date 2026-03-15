namespace Video.Domain.ValueObjects;

public record TimeSpace(DateTimeOffset CreateAt, DateTimeOffset UpdateAt)
{
    public DateTimeOffset CreateAt { get; init; }
    public DateTimeOffset UpdateAt { get; private set; }
    
    public void ResetUpdateAt(DateTimeOffset updateAts)
    {
        UpdateAt = updateAts;
    }
}