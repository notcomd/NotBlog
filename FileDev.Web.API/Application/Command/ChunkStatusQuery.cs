namespace FileDev.Web.API.Application.Command;

public class ChunkStatusQuery : IRequest<ChunkStatusResponse>
{
    public string FileKey { get; set; } = null!;
}

public class ChunkStatusResponse
{
    public string FileKey { get; set; } = null!;
    public int TotalChunks { get; set; }
    public List<int> UploadedChunks { get; set; } = [];
    public bool IsComplete { get; set; }
    public ChunkUploadStatus Status { get; set; }
}
