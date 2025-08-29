namespace Identity.Web.API.Application.Models;

public record RequestLogInWithEmailModel(string LoginEmail,string HashPassword,string GenerateCode);

