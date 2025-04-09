using Grpc.Core;

namespace FileGrpcService.Services;

public class GrpcFileService : GrpcFile.GrpcFileBase
{
    private readonly ILogger<GrpcFileService> _grpcLogger;

    public GrpcFileService(ILogger<GrpcFileService> grpcLogger)
    {
        _grpcLogger = grpcLogger;
    }

    public override Task<FileResponse> UploadFileAsync(IAsyncStreamReader<FileRequest> requestStream, ServerCallContext context)
    {
        return Task.FromResult(new FileResponse
        {
            FileName = "FileName",
            FileContent = "FileContent"
        });
    }
}