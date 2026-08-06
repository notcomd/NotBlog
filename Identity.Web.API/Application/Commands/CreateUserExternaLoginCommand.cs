using Identity.Domain.Entities.UserExternalLoginAggregate;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 创建外部登录绑定命令（F-07 修复版）
/// 强类型 Provider（不再硬编码 GitHub）、必须关联本地用户（LinkUser）、令牌加密存储
/// </summary>
public record CreateUserExternalLoginCommand(
    LoginProviderType Provider,
    Guid UserId,
    string ProviderKey,
    string ProviderDisplayName,
    string? ProviderUnionId = null,
    string? ProviderAccessToken = null,
    string? ProviderRefreshToken = null,
    DateTimeOffset? ProviderExpiresAt = null
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(ProviderKey);
    public string IdValue => ProviderKey;
}
