
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

    Task<CircleInvitation> AddAsync(CircleInvitation invitation);
    Task<CircleInvitation> UpdateAsync(CircleInvitation invitation);
    Task DeleteAsync(Guid inviteGuid);
}
