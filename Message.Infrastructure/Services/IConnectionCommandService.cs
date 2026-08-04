namespace Message.Infrastructure.Services;

/// <summary>
/// 连接命令服务接口（命令侧）。
/// <para>
/// 依据 CQRS 职责分离原则，本接口承载连接登记/注销与在线状态写入等<b>命令性</b>操作，
/// 与领域层查询接口 <see cref="Message.Domain.IServices.IConnectionManager"/> 分离。
/// </para>
/// </summary>
public interface IConnectionCommandService
{
    /// <summary>
    /// 登记用户连接
    /// </summary>
    Task AddConnectionAsync(Guid userId, string connectionId);

    /// <summary>
    /// 注销用户连接
    /// </summary>
    Task RemoveConnectionAsync(Guid userId, string connectionId);

    /// <summary>
    /// 将用户置为在线
    /// </summary>
    Task SetUserOnlineAsync(Guid userId);

    /// <summary>
    /// 将用户置为离线
    /// </summary>
    Task SetUserOfflineAsync(Guid userId);
}
