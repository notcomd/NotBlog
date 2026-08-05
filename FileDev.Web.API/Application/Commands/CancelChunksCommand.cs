namespace FileDev.Web.API.Application.Commands;


public class CancelChunksCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string FileKey { get; set; } = null!;
}
