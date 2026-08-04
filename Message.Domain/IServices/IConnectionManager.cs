namespace Message.Domain.IServices;

/// <summary>
/// 连接查询服务接口（查询侧）。
/// <para>
/// 依据 CQRS 职责分离原则，本接口<b>仅</b>提供与连接/在线状态相关的<b>查询</b>能力；
/// 连接登记、注销与在线状态写入等命令性操作由基础设施层
/// <c>Message.Infrastructure.Services.IConnectionCommandService</c> 承载。
/// </para>
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// 获取用户当前全部在线连接 ID
    /// </summary>
    Task<IEnumerable<string>> GetConnectionsAsync(Guid userId);

    /// <summary>
    /// 判断用户是否还有其他在线连接
    /// </summary>
    Task<bool> HasOtherConnectionsAsync(Guid userId);

    /// <summary>
    /// 判断用户是否在线
    /// </summary>
    Task<bool> IsUserOnlineAsync(Guid userId);

    /// <summary>
    /// 获取当前在线用户数
    /// </summary>
    Task<int> GetOnlineCountAsync();
}
