namespace FileDev.Web.API.Application.Command;


public class CancelChunksCommand : IRequest<bool>
{
    public string FileKey { get; set; } = null!;
}
