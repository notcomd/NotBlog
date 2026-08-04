namespace Identity.Infrastructure.Idempotent;

/// <summary>
/// 请求管理接口
/// </summary>
public interface IRequestManagement
{
    /// <summary>
    /// 执行请求
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <returns>是否成功执行</returns>
    Task<bool> ExecuteAsync(Guid request);

    /// <summary>
    /// 为命令创建幂等请求记录。
    /// 依赖 ClientRequestId 唯一约束保证并发安全：并发相同 request 同时插入时，仅一个成功，
    /// 其余捕获 DbUpdateException 后返回 false（视为重复请求）。
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <typeparam name="T">命令类型</typeparam>
    /// <returns>true=新建成功可继续执行；false=已存在（重复请求）</returns>
    Task<bool> CreateRequestForCommandAsync<T>(Guid request);

    /// <summary>
    /// 删除幂等请求记录（S-14：命令执行失败时回滚，允许客户端重试）
    /// </summary>
    /// <param name="request">请求 ID</param>
    Task RemoveRequestAsync(Guid request);
}