namespace Identity.Web.API.Application.Commands;

public record CreateUserExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string ProviderDisplayName,
    string? ProviderUnionId,
    string? ProviderAccessToken,
    string? ProviderRefreshToken,
    string? ProviderExpiresAt
) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(ProviderKey);
    public string IdValue => ProviderKey;
}