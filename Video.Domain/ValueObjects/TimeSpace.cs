namespace Video.Domain.ValueObjects;

public record TimeSpace(DateTimeOffset _CreateAt, DateTimeOffset _UpdateAt)
{
    public DateTimeOffset CreateAt { get; init; }
    public DateTimeOffset UpdateAt { get; private set; }
    
    public void ResetUpdateAt(DateTimeOffset updateAts)
    {
        UpdateAt = updateAts;
    }
}