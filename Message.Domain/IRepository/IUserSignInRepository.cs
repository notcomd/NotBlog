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

    Task<UserSignIn> AddAsync(UserSignIn signIn);
}
