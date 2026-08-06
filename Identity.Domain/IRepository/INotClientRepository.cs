using Identity.Domain.Entities.ClientAggregate;

namespace Identity.Domain.IRepository;

/// <summary>
/// OAuth 客户端（NotClient）仓储接口 — 支持 RFC 6749 / RFC 7591 客户端注册与查询
/// </summary>
public interface INotClientRepository
{
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// 按 OAuth client_id 查找客户端（排除已吊销）
    /// </summary>
    ValueTask<NotClient?> FindByClientIdAsync(string clientId, CancellationToken ct = default);

    /// <summary>
    /// 按内部主键查找客户端
    /// </summary>
    ValueTask<NotClient?> FindByIdAsync(Guid notClientId, CancellationToken ct = default);

    /// <summary>
    /// 获取全部客户端
    /// </summary>
    Task<IReadOnlyList<NotClient>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 添加客户端
    /// </summary>
    ValueTask AddAsync(NotClient client, CancellationToken ct = default);

    /// <summary>
    /// 更新客户端
    /// </summary>
    ValueTask UpdateAsync(NotClient client, CancellationToken ct = default);
}
