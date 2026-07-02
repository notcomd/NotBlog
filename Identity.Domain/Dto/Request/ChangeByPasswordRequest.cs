namespace Identity.Domain.Dto.Request;

public record ChangeByPasswordRequest(string Email, string Password, string NewPassword, string Code);