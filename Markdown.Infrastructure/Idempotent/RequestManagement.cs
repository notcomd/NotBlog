using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Markdown.Infrastructure.Idempotent;

public class RequestManagement(MarkDownDbContext context) : IRequestManagement
{
    private readonly MarkDownDbContext _context = context ?? throw new ArgumentNullException(nameof(context));


    public async Task<bool> ExecuteAsync(Guid id)
    {
        var data = await _context.FindAsync<ClientRequest>(id);
        return data != null;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        // 直接插入并依赖 ClientRequestId 主键唯一约束做并发去重：
        // 并发重复请求（相同 IdempotencyKey）中只有一条能插入成功，其余捕获唯一键冲突后忽略，
        // 避免了"查→插"两步之间的 TOCTOU 竞态窗口。
        _context.Add(new ClientRequest
        {
            ClientRequestId = id,
            ClientRequestName = typeof(T).Name,
            Created = DateTimeOffset.UtcNow,
        });
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 幂等命中：同一 IdempotencyKey 已被处理，忽略本次重复请求
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}