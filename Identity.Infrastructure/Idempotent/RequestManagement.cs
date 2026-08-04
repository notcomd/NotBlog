namespace Identity.Infrastructure.Idempotent;

public class RequestManagement(IdentityDbContext context) : IRequestManagement
{
    private readonly IdentityDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// 执行请求
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <returns>是否成功执行</returns>
    public async Task<bool> ExecuteAsync(Guid request)
    {
        var data = await _context.FindAsync<ClientRequest>(request);
        return data != null;
    }

    /// <summary>
    /// 删除幂等请求记录（S-14：命令失败时回滚，允许客户端重试）
    /// </summary>
    public async Task RemoveRequestAsync(Guid request)
    {
        var data = await _context.FindAsync<ClientRequest>(request);
        if (data is not null)
        {
            _context.ClientRequests.Remove(data);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 为命令创建幂等请求记录（并发安全）。
    /// 直接尝试插入，依赖 ClientRequestId 唯一约束：并发相同 request 同时插入时，仅一个成功，
    /// 其余捕获 DbUpdateException 返回 false（视为重复）。移除了原先"先查后插"的 TOCTOU 竞态。
    /// </summary>
    /// <returns>true=新建成功；false=已存在（重复请求）</returns>
    public async Task<bool> CreateRequestForCommandAsync<T>(Guid request)
    {
        var request1 = new ClientRequest
        {
            ClientRequestId = request,
            ClientRequestName = typeof(T).Name,
            CreatedDate = DateTimeOffset.UtcNow
        };
        try
        {
            await _context.AddAsync(request1);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // 并发下另一个相同 request 已抢先插入（主键冲突），视为重复请求
            return false;
        }
    }
}