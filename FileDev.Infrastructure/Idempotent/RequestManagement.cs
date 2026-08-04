using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Idempotent;

public class RequestManagement(NotFileDbContext notFileDbContext) : IRequestManagement
{
    private readonly NotFileDbContext _notFileDbContext =
        notFileDbContext ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public async Task<bool> ExecuteAsync(Guid request)
    {
        var clientRequest = await _notFileDbContext.FindAsync<ClientRequest>(request);
        return clientRequest != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid request)
    {
        // 直接尝试插入，依赖 ClientRequestId 主键唯一约束：并发下相同 request 同时插入时，
        // 仅一个成功，其余捕获 DbUpdateException 视为重复请求（消除"先查后插"的 TOCTOU 竞态）
        var requset = new ClientRequest
        {
            ClientRequestId = request,
            ClientRequestName = typeof(T).Name,
            CreatedDate = DateTimeOffset.UtcNow
        };
        try
        {
            _notFileDbContext.Add(requset);
            await _notFileDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 并发下另一个相同 request 已抢先插入（主键冲突），视为重复请求
        }
    }
}