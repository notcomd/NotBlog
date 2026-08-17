namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 创建 OAuth 客户端：校验 client_id 唯一与认证方式取值，服务端生成 client_id/client_secret
/// </summary>
public class CreateNotClientCommandHandler(
    INotClientRepository clientRepository,
    ILogger<CreateNotClientCommandHandler> logger)
    :  IRequestHandler<CreateNotClientCommand, CreateNotClientResult>
{
    /// <summary>Token 端点认证方式白名单（RFC 6749 §2.3.1 / RFC 7591）</summary>
    private static readonly HashSet<string> AllowedAuthMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "client_secret_basic", "client_secret_post", "private_key_jwt", "none"
    };

    public async Task<CreateNotClientResult> Handler(CreateNotClientCommand command, CancellationToken ct)
    {
        // ── 1. client_id：调用方可选传入，不传则由服务端生成 ──
        var clientId = command.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = $"app_{JwtRandom.GenerateRandomString(16)}";
        }
        else if (await clientRepository.FindByClientIdAsync(clientId, ct) is not null)
        {
            throw new InvalidOperationException($"client_id '{clientId}' 已存在");
        }

        // ── 2. 认证方式白名单校验 ──
        if (!AllowedAuthMethods.Contains(command.TokenEndpointAuthMethod))
        {
            throw new InvalidOperationException(
                $"TokenEndpointAuthMethod 仅支持: {string.Join(", ", AllowedAuthMethods)}");
        }

        // ── 3. 服务端生成强随机密钥（仅返回一次）──
        var clientSecret = JwtRandom.GenerateRandomString(48);

        var client = new NotClient(
            clientId,
            command.ApplicationName,
            clientSecret,
            command.RedirectUris,
            command.AllowedGrantTypes,
            command.AllowedScopes,
            command.ApplicationType,
            command.TokenEndpointAuthMethod,
            command.ApplicationDescription,
            command.ApplicationIcon,
            command.HomepageUri,
            command.PrivacyPolicyUri,
            command.TermsOfServiceUri,
            command.ContactEmail,
            command.PostLogoutRedirectUris,
            command.AllowedCorsOrigins,
            command.RequirePkce,
            command.RequireConsent);

        await clientRepository.AddAsync(client, ct);
        await clientRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreateNotClient] 创建成功: ClientId={ClientId}, Id={Id}",
            client.ClientId, client.NotClientId);

        return new CreateNotClientResult(client.NotClientId, client.ClientId, client.ClientSecret);
    }
}
