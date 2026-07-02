namespace CacheMemory.Core;

/// <summary>
/// Redis 操作重试策略接口。
/// 封装可配置的指数退避重试逻辑，可应用于所有 Redis 操作。
/// </summary>
public interface IRedisRetryPolicy
{
    /// <summary>
    /// 使用重试策略执行带返回值的异步操作。
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="action">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用重试策略执行不带返回值的异步操作。
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用重试策略执行带返回值的同步操作。
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="action">要执行的操作</param>
    /// <returns>操作结果</returns>
    T Execute<T>(Func<T> action);

    /// <summary>
    /// 使用重试策略执行不带返回值的同步操作。
    /// </summary>
    /// <param name="action">要执行的操作</param>
    void Execute(Action action);
}