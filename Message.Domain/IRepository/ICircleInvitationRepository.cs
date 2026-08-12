
namespace Message.Domain.IRepository;

public interface ICircleInvitationRepository : IRepository<CircleInvitation, IUnitOfWork>
{
    Task<CircleInvitation?> GetByIdAsync(Guid inviteGuid);

    /// <summary>按邀请码查询（未失效校验由命令层完成）</summary>
    Task<CircleInvitation?> GetByCodeAsync(string code);

    /// <summary>按链接 token 查询</summary>
    Task<CircleInvitation?> GetByTokenAsync(Guid token);

    /// <summary>圈子的邀请列表（圈主/管理员视角，分页）</summary>
    Task<IEnumerable<CircleInvitation>> GetByCircleAsync(Guid circleGuid, int page = 1, int pageSize = 20);

    /// <summary>圈子邀请总数</summary>
    Task<int> GetCountByCircleAsync(Guid circleGuid);

    /// <summary>用户收到的直邀列表（分页）</summary>
    Task<IEnumerable<CircleInvitation>> GetByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true, int page = 1, int pageSize = 20);

    /// <summary>用户收到的直邀总数</summary>
    Task<int> GetCountByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true);

    /// <summary>邀请码是否已被占用（生成时查重）</summary>
    Task<bool> CodeExistsAsync(string code);

    /// <summary>
    /// 原子占用邀请（仅 Pending 且未过期时置为 Accepted），返回是否占用成功。
    /// <para>邀请码一次性使用：并发下两个请求同时使用同一邀请时只有一个成功（条件更新，防重复入圈）。</para>
    /// </summary>
    Task<bool> TryAcceptAtomicallyAsync(Guid inviteGuid);

    /// <summary>统计指定邀请人在时间窗口内创建的邀请码数量（周额度校验用，仅统计 Type=Code）</summary>
    Task<int> CountCodesCreatedSinceAsync(Guid inviterGuid, DateTimeOffset since);

    Task<CircleInvitation> AddAsync(CircleInvitation invitation);
    Task<CircleInvitation> UpdateAsync(CircleInvitation invitation);
    Task DeleteAsync(Guid inviteGuid);
}
