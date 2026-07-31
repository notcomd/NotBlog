namespace Identity.Domain.Dto.Request;

public record RegisterRequest([EmailAddress(ErrorMessage = "Error Email Address!")]string UserEmail, string UserPassword, string VerificationCode);
