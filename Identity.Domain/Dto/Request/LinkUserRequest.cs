namespace Identity.Domain.Dto.Request;

public record LinkUserRequest(string Provider, string ProviderKey, string Email, string Code);