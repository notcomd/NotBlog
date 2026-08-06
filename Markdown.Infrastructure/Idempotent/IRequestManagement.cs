namespace Markdown.Infrastructure.Idempotent;

/// <summary>
///     命令幂等执行管理（基于 ClientRequest 表 + ClientRequestId 主键唯一约束）：
///     原子占位防并发重复（TOCTOU 安全），首次执行结果落库，重复请求返回首次结果
/// </summary>
public interface IRequestManagement
{
    /// <summary>
    ///     幂等执行入口：先原子占位（唯一约束冲突 = 并发重复请求），占位成功者执行 operation 并写入响应；
    ///     占位失败者（输家）轮询等待赢家写入响应后返回首次执行结果
    /// </summary>
    /// <typeparam name="T">命令返回类型</typeparam>
    /// <param name="id">幂等 key（IdempotencyKey）</param>
    /// <param name="operation">命令业务执行委托</param>
    /// <returns>本次或首次执行的命令结果</returns>
    Task<T> ExecuteIdempotentAsync<T>(Guid id, Func<Task<T>> operation);

    /// <summary>
    ///     查询指定幂等 key 是否已处理；已处理时返回首次执行时存储的响应
    /// </summary>
    Task<(bool Handled, T? Response)> ExecuteAsync<T>(Guid id);

    /// <summary>
    ///     原子插入幂等占位记录（不携带响应），返回是否插入成功；
    ///     返回 false 表示同一 IdempotencyKey 已被并发请求占用（唯一约束冲突）
    /// </summary>
    Task<bool> CreateRequestForCommandAsync<T>(Guid id);

    /// <summary>
    ///     为已占位的幂等记录写入命令执行结果（仅占位成功的请求调用）
    /// </summary>
    Task UpdateResponseAsync<T>(Guid id, T? response);
}
