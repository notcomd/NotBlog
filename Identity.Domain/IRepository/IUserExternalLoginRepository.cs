using Identity.Domain.Entities.UserExternalLoginAggregate;

namespace Identity.Domain.IRepository;

public interface IUserExternalLoginRepository : IRepository<UserExternalLogin>
{
    /// <summary>
    /// 添加外部登录记录
    /// </summary>
    Task AddAsync(UserExternalLogin login);

    /// <summary>
    /// 按用户 ID 查找所有外部登录
    /// </summary>
    Task<IReadOnlyList<UserExternalLogin>> FindByUserIdAsync(Guid userId);

    /// <summary>
    /// 按提供商 + 提供商密钥查找（用于检查是否已存在）
    /// </summary>
    Task<UserExternalLogin?> FindByProviderAsync(LoginProviderType provider, string providerKey);

    /// <summary>
    /// 按用户 ID + 提供商查找
    /// </summary>
    Task<UserExternalLogin?> FindByUserIdAndProviderAsync(Guid userId, LoginProviderType provider);

    /// <summary>
    /// 获取所有外部登录记录
    /// </summary>
    Task<IReadOnlyList<UserExternalLogin>> GetAllAsync();

    /// <summary>
    /// 删除外部登录绑定记录（F-07：真实解绑链路）
    /// </summary>
    Task DeleteAsync(UserExternalLogin login);


    Task<UserExternalLogin?> FindOneByUserIdAndProviderAsync(LoginProviderType provider, string providerKey);
}