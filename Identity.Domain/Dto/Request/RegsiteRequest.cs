namespace Identity.Domain.Dto.Request;

public record RegisterRequest(string UserPassword, string VerificationCode, string UserEmail);