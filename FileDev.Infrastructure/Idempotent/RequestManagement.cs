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

    /// <summary>
    /// 为命令创建幂等请求记录（insert-or-detect，并发安全）。
    /// 直接尝试插入，依赖 ClientRequestId 主键唯一约束：并发相同 request 同时插入时仅一个成功，
    /// 其余捕获 DbUpdateException 返回 false（视为重复），消除「先查后插」的 TOCTOU 竞态窗口。
    /// </summary>
    /// <returns>true=新建成功；false=已存在（重复请求）</returns>
    public async Task<bool> CreateRequestForCommandAsync<T>(Guid request)
    {
        var requestRecord = new ClientRequest
        {
            ClientRequestId = request,
            ClientRequestName = typeof(T).Name,
            CreatedDate = DateTimeOffset.UtcNow
        };
        try
        {
            _notFileDbContext.Add(requestRecord);
            await _notFileDbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // 并发下另一个相同 request 已抢先插入（主键冲突），视为重复请求
            return false;
        }
    }

    /// <summary>
    /// 删除幂等请求记录（S-14：命令执行失败时回滚，允许客户端使用同一幂等键重试）
    /// </summary>
    public async Task RemoveRequestAsync(Guid request)
    {
        var data = await _notFileDbContext.FindAsync<ClientRequest>(request);
        if (data is not null)
        {
            _notFileDbContext.Remove(data);
            await _notFileDbContext.SaveChangesAsync();
        }
    }
}
