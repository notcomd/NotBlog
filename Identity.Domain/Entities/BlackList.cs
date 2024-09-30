namespace Identity.Domain.Entities;

public class BlackList:IAggregateRoot
{
    public Guid BlackGuid { get; init; }
    public PhoneNumber? PhoneNumber { get; set; }
    public string? Email { get; private set; }
    public DateTime LimitTime { get; init; }
    public string? Message { get; set; }
    
    private BlackList(){}

    public BlackList(PhoneNumber phoneNumber,string email,DateTime limitTime,string message)
    {
        BlackGuid = new Guid();
        PhoneNumber = phoneNumber;
        Email = email;
        LimitTime = limitTime;
        Message = message;
    }
    
    
    
}