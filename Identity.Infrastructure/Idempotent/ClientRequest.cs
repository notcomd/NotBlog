namespace Identity.Infrastructure.Idempotent;

public class ClientRequest
{
    public Guid ClientRequestId { get; set; }
    public string ClientRequestName { get; set; } = null!;
    public DateTimeOffset CreatedDate { get; set; }
}