namespace FileDev.Infrastructure.Idempotent;

public interface IRequestManagement
{
    Task<bool> ExecuteAsync(Guid request);

    /// <summary>
    /// 为命令创建幂等请求记录（insert-or-detect）。
    /// </summary>
    /// <returns>true=新建成功；false=已存在（重复请求，并发下由主键唯一约束兜底）</returns>
    Task<bool> CreateRequestForCommandAsync<T>(Guid request);

    /// <summary>
    /// 删除幂等请求记录（S-14：命令执行失败时回滚，允许客户端使用同一幂等键重试）
    /// </summary>
    Task RemoveRequestAsync(Guid request);
}
