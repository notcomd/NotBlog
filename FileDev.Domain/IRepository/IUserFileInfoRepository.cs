using FileDev.Domain.Entities;

namespace FileDev.Domain.IRepository;

/// <summary>
/// 用户存储额度仓储。每个用户一条额度记录（UserId 主键），
/// 用于配额校验（读取/记账）与额度动态调整。
/// <para>
/// 占用/释放使用数据库端原子 UPDATE（ExecuteUpdate）保证并发下不丢失更新；
/// 初始化采用唯一主键冲突幂等处理，支持存量用户惰性建账。
/// </para>
/// </summary>
public interface IUserFileInfoRepository : IRepository<UserFileInfo, IUnitOfWork>
{
    /// <summary>按用户获取额度记录；不存在返回 null。</summary>
    /// <param name="userId">用户 ID。</param>
    Task<UserFileInfo?> GetByUserIdAsync(Guid userId);

    /// <summary>插入额度记录（新用户首次初始化）。</summary>
    /// <param name="userFileInfo">额度记录。</param>
    Task InsertUserFileInfoAsync(UserFileInfo userFileInfo);

    /// <summary>
    /// 幂等初始化额度记录：不存在则插入并落库，存在则返回已存在记录。
    /// 并发首插主键冲突时自动降级为读取已存在记录。
    /// </summary>
    /// <param name="userFileInfo">待插入的额度记录。</param>
    Task<UserFileInfo> EnsureUserFileInfoAsync(UserFileInfo userFileInfo);

    /// <summary>
    /// 原子占用额度（单条 UPDATE，条件内联配额判断）：未超限则 +bytes 并返回 true；
    /// 超限或记录不存在返回 false。用于上传成功后记账，杜绝并发丢失更新。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="bytes">占用字节数（非正数直接返回 true）。</param>
    Task<bool> TryOccupyAsync(Guid userId, long bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 原子释放额度（单条 UPDATE，结果不低于 0）：用于删除文件后回退。
    /// 记录不存在时忽略（返回 false）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="bytes">释放字节数（非正数直接返回 true）。</param>
    Task<bool> ReleaseAsync(Guid userId, long bytes, CancellationToken cancellationToken = default);
}
