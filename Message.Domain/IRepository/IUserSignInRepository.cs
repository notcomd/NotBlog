namespace Message.Domain.IRepository;

/// <summary>
/// 用户签到记录仓储（UserSignIn 为普通实体，非聚合根；命令层保存时配合 IUnitOfWork）。
/// </summary>
public interface IUserSignInRepository
{
    //IUnitOfWork UnitOfWork { get; }

    /// <summary>查询指定用户指定日期的签到记录（不存在返回 null）</summary>
    Task<UserSignIn?> GetAsync(Guid userId, DateOnly date);

    /// <summary>指定用户指定日期是否已签到</summary>
    Task<bool> IsSignedInAsync(Guid userId, DateOnly date);

    /// <summary>
    /// 查询指定用户在 [from, to] 闭区间内的签到日期（按日期升序、去重）。
    /// <para>供签到热力图按天渲染使用。</para>
    /// </summary>
    Task<IReadOnlyList<DateOnly>> GetDatesAsync(Guid userId, DateOnly from, DateOnly to);

    /// <summary>指定用户累计签到天数（全部历史，非区间内）</summary>
    Task<int> CountAsync(Guid userId);

    /// <summary>新增签到记录</summary>
    Task<UserSignIn> AddAsync(UserSignIn signIn);
}
