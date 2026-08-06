using Identity.Domain.Entities.UserExternalLoginAggregate;
using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 创建外部登录绑定（F-07 修复版）：
///   1. 按命令携带的 Provider 查重（不再硬编码 GitHub）
///   2. 绑定记录必须关联本地用户（LinkUser，此前 UserId 恒为空）
///   3. 外部令牌 AES-256-GCM 加密后落库（此前明文存储）
/// </summary>
public sealed class CreateUserExternalLoginCommandHandler(
    IUserExternalLoginRepository userExternalLoginRepository,
    ITokenEncryptionService tokenEncryptionService) : IRequestHandler<CreateUserExternalLoginCommand, bool>
{
    public async Task<bool> Handler(CreateUserExternalLoginCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("UserId 不能为空", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ProviderKey))
            throw new ArgumentException("ProviderKey 不能为空", nameof(request));

        // 同 provider 下 ProviderKey 唯一（DB 唯一索引 (Provider, ProviderKey) 兜底）
        var data = await userExternalLoginRepository
            .FindByProviderAsync(request.Provider, request.ProviderKey);
        if (data is not null)
        {
            return false;
        }

        data = UserExternalLogin.Create(
            request.Provider,
            request.ProviderKey,
            request.ProviderDisplayName,
            request.ProviderUnionId);

        // 真实关联本地用户
        data.LinkUser(request.UserId);

        // 外部令牌加密存储（任一令牌存在时才写入；过期时间由调用方显式传入）
        if (!string.IsNullOrWhiteSpace(request.ProviderAccessToken) ||
            !string.IsNullOrWhiteSpace(request.ProviderRefreshToken))
        {
            data.UpdateTokens(
                string.IsNullOrWhiteSpace(request.ProviderAccessToken)
                    ? string.Empty
                    : tokenEncryptionService.Encrypt(request.ProviderAccessToken),
                string.IsNullOrWhiteSpace(request.ProviderRefreshToken)
                    ? null
                    : tokenEncryptionService.Encrypt(request.ProviderRefreshToken),
                request.ProviderExpiresAt);
        }

        await userExternalLoginRepository.AddAsync(data);
        await userExternalLoginRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}
