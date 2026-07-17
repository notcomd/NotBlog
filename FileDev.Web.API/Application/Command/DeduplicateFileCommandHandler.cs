namespace FileDev.Web.API.Application.Command;

using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

public class DeduplicateFileCommandHandler(
    NotFileDbContext dbContext,
    ILogger<DeduplicateFileCommandHandler> logger)
    : NotMediator.IRequestHandler<DeduplicateFileCommand, DeduplicateFileResponse>
{
    public async Task<DeduplicateFileResponse> Handler(DeduplicateFileCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.NotFiles
            .Where(f => f.FileMd5 == request.FileMd5 && !f.IsDeleted)
            .Select(f => new DeduplicateFileResponse
            {
                Exists = true,
                FileId = f.FileId,
                FileUri = f.FileUri.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            logger.LogInformation("[Dedup] 文件已存在（秒传）: Md5={Md5}, FileId={FileId}",
                request.FileMd5, existing.FileId);
            return existing;
        }

        return new DeduplicateFileResponse { Exists = false };
    }
}
