namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 更新 OAuth 客户端信息（已吊销客户端拒绝更新）
/// </summary>
public class UpdateNotClientCommandHandler(
    INotClientRepository clientRepository,
    ILogger<UpdateNotClientCommandHandler> logger)
    : NotMediator.IRequestHandler<UpdateNotClientCommand, bool>
{
    public async Task<bool> Handler(UpdateNotClientCommand command, CancellationToken ct)
    {
        var client = await clientRepository.FindByIdAsync(command.NotClientId, ct)
            ?? throw new InvalidOperationException($"客户端 '{command.NotClientId}' 不存在");

        if (client.Status == ClientStatus.Revoked)
            throw new InvalidOperationException("已吊销的客户端无法更新，请重新创建");

        client.UpdateApplicationInfo(
            command.ApplicationName,
            command.ApplicationDescription,
            command.ApplicationIcon,
            command.HomepageUri,
            command.PrivacyPolicyUri,
            command.TermsOfServiceUri,
            command.ContactEmail);

        if (command.RedirectUris is not null)
            client.UpdateRedirectUris(command.RedirectUris);

        if (command.AllowedGrantTypes is not null)
            client.UpdateGrantTypes(command.AllowedGrantTypes);

        if (command.AllowedScopes is not null)
            client.UpdateScopes(command.AllowedScopes);

        if (command.AllowedCorsOrigins is not null)
            client.UpdateCorsOrigins(command.AllowedCorsOrigins);

        await clientRepository.UpdateAsync(client, ct);
        await clientRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdateNotClient] 更新成功: Id={Id}, ClientId={ClientId}",
            command.NotClientId, client.ClientId);
        return true;
    }
}
