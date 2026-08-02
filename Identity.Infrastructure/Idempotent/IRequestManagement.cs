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
    /// 创建为命令创建请求
    /// </summary>
    /// <param name="request">请求 ID</param>
    /// <typeparam name="T">命令类型</typeparam>
    /// <returns>是否成功创建</returns>
    Task CreateRequestForCommandAsync<T>(Guid request);

    /// <summary>
    /// 删除幂等请求记录（S-14：命令执行失败时回滚，允许客户端重试）
    /// </summary>
    /// <param name="request">请求 ID</param>
    Task RemoveRequestAsync(Guid request);
}