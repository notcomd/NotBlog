namespace Message.Web.API.Dto.Request;
public class ForwardMessageRequest
{
    public Guid TargetSessionId { get; init; }
    public ForwardType ForwardType { get; init; }
    public string? Comment { get; init; }
}

