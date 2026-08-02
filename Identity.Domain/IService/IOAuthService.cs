using Identity.Domain.Dto.OAuth;
using Identity.Domain.Entities.UserExternalLoginAggregate;

namespace Identity.Domain.IService;

public interface IOAuthService
{
    /// <summary>
    /// 生成外部登录授权URL
    /// </summary>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="redirectUri">回调URL</param>
    /// <returns>授权URL</returns>
    Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri);

    /// <summary>
    /// 处理外部登录回调
    /// </summary>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">回调URL</param>
    /// <param name="state">OAuth 状态参数（CSRF 校验用，可空以兼容旧调用方）</param>
    /// <returns>登录响应</returns>
    Task<OAuthLoginResponse> HandleCallbackAsync(string provider, string code, string redirectUri,
        string? state = null);

    /// <summary>
    /// 获取已存在的外部登录用户
    /// </summary>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="providerUserId">提供程序用户ID</param>
    /// <returns>用户实体</returns>
    Task<User?> GetExistingUserByExternalLoginAsync(string provider, string providerUserId);

    /// <summary>
    /// 创建或更新外部登录用户
    /// </summary>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="externalUserInfo">外部用户信息</param>
    /// <returns>用户实体</returns>
    Task<User> CreateOrUpdateUserFromExternalLoginAsync(string provider, ExternalUserInfo externalUserInfo);

    /// <summary>
    /// 关联外部登录用户到指定用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="providerUserId">提供程序用户ID</param>
    /// <param name="displayName">用户显示名称</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    Task LinkExternalLoginToUserAsync(Guid userId, string provider, string providerUserId, string displayName);

    /// <summary>
    /// 解除外部登录用户与指定用户的关联
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="providerUserId">提供程序用户ID</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    Task UnlinkExternalLoginFromUserAsync(Guid userId, string provider, string providerUserId);

    /// <summary>
    /// 通过 OAuth 授权码将外部账号绑定到当前用户（F-07）
    /// </summary>
    /// <param name="userId">本地用户 ID（从已认证的 NameIdentifier Claim 获取）</param>
    /// <param name="provider">外部登录提供程序</param>
    /// <param name="code">OAuth 授权码</param>
    /// <param name="redirectUri">回调地址</param>
    Task LinkExternalLoginByCodeAsync(Guid userId, string provider, string code, string redirectUri);

    /// <summary>
    /// 获取用户已绑定的外部登录账号列表（F-07）
    /// </summary>
    Task<IReadOnlyList<UserExternalLogin>> GetLinkedAccountsAsync(Guid userId);
}