
namespace FileDev.Domain.Dto.Request;


public record ChunkInitRequest(
    string FileName,
    long TotalSize,
    int ChunkSize = 5242880,
    string? FileMd5 = null,
    bool IsPublic = false,
    string? Description = null);