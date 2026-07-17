namespace FileDev.Web.API.Application.Command;

public class ChunkStatusQuery : IRequest<ChunkStatusResponse>
{
    public string FileKey { get; set; } = null!;
}

public class ChunkStatusResponse
{
    public string FileKey { get; set; } = null!;
    public int TotalChunks { get; set; }
    public HashSet<int> UploadedChunks { get; set; } = new();
    public bool IsComplete { get; set; }
    public ChunkUploadStatus Status { get; set; }
}
