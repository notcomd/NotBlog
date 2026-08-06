namespace Identity.Web.API.Application.Commands.Client;

/// <summary>
/// 吊销 OAuth 客户端（Status -> Revoked，/token 与 /authorize 随即拒绝）
/// </summary>
public class RevokeNotClientCommandHandler(
    INotClientRepository clientRepository,
    ILogger<RevokeNotClientCommandHandler> logger)
    : NotMediator.IRequestHandler<RevokeNotClientCommand, bool>
{
    public async Task<bool> Handler(RevokeNotClientCommand command, CancellationToken ct)
    {
        var client = await clientRepository.FindByIdAsync(command.NotClientId, ct)
            ?? throw new InvalidOperationException($"客户端 '{command.NotClientId}' 不存在");

        client.Revoke();
        await clientRepository.UpdateAsync(client, ct);
        await clientRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[RevokeNotClient] 已吊销: Id={Id}, ClientId={ClientId}",
            command.NotClientId, client.ClientId);
        return true;
    }
}
