namespace Identity.Web.API.ResponseEntites;

public record LoginResponse(string UserAccount, string PasswordHash, string Code);