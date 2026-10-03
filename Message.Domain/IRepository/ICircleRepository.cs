
namespace Message.Domain.IRepository;

/// <summary>
/// 圈子仓储接口（Circle 聚合根）。
/// </summary>
public interface ICircleRepository : IRepository<Circle, IUnitOfWork>
{
    /// <summary>获取圈子（不含成员列表）</summary>
    Task<Circle?> GetByIdAsync(Guid circleGuid);

    /// <summary>获取圈子及其成员列表（需要修改成员的场景）</summary>
    Task<Circle?> GetByIdWithMembersAsync(Guid circleGuid);

    /// <summary>获取用户加入的圈子（Active）</summary>
    Task<IEnumerable<Circle>> GetByMemberAsync(Guid userId);

    /// <summary>获取用户创建的圈子</summary>
    Task<IEnumerable<Circle>> GetByOwnerAsync(Guid ownerGuid);

    /// <summary>活跃圈子列表（R-12 圈子发现；keyword 模糊匹配名称，按创建时间倒序分页）</summary>
    Task<IEnumerable<Circle>> GetActiveAsync(string? keyword, int page = 1, int pageSize = 20);

    /// <summary>活跃圈子总数（R-12，与 GetActiveAsync 同条件）</summary>
    Task<int> GetActiveCountAsync(string? keyword);

    /// <summary>轻量成员校验（DB 查询，不加载整个圈子）</summary>
    Task<bool> IsMemberAsync(Guid circleGuid, Guid userId);

    /// <summary>查询单个成员（读操作）</summary>
    Task<CircleMember?> GetMemberAsync(Guid circleGuid, Guid userId);

    /// <summary>成员列表（读操作，分页）</summary>
    Task<IEnumerable<CircleMember>> GetMembersAsync(Guid circleGuid, int page = 1, int pageSize = 50);

    /// <summary>成员总数</summary>
    Task<int> GetMemberCountAsync(Guid circleGuid);

    /// <summary>新增圈子</summary>
    Task<Circle> AddAsync(Circle circle);
    /// <summary>更新圈子</summary>
    Task<Circle> UpdateAsync(Circle circle);
    /// <summary>删除圈子</summary>
    Task DeleteAsync(Guid circleGuid);
    /// <summary>判断圈子是否存在</summary>
    Task<bool> ExistsAsync(Guid circleGuid);

    /// <summary>管理端全量圈子分页（含已解散，keyword 模糊匹配名称，按创建时间倒序）</summary>
    Task<IEnumerable<Circle>> GetPagedAsync(string? keyword, int page = 1, int pageSize = 20);

    /// <summary>管理端全量圈子总数（含已解散，与 GetPagedAsync 同条件）</summary>
    Task<int> GetTotalCountAsync(string? keyword);
}
