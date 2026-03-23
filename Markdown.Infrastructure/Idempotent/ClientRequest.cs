namespace Markdown.Infrastructure.Idempotent;

public class ClientRequest
{
    public Guid ClientRequestId { get; set; }
    public string ClientRequestName { get; set; } = null!;
    public DateTimeOffset Created { get; set; }
}