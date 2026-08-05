namespace FileDev.Web.API.Application.Queries;

public class ChunkStatusQuery : IRequest<ChunkStatusResponse>
{
    public Guid UserId { get; set; }
    public string FileKey { get; set; } = null!;
}
