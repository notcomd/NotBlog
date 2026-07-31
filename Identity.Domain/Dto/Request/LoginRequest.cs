namespace Identity.Domain.Dto.Request;

public record LoginRequest(
    string Email,
    string Password,
    string Code,
    string Provider,
    string RedirectUri,
    string ClientId,
    string ClientSecret,
    string GrantType);
