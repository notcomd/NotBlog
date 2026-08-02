namespace FileDev.Web.API.Application.Command;

public class UploadChunkCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string FileKey { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public byte[] ChunkContent { get; set; } = null!;
    public string? ChunkHash { get; set; }
}
