namespace Identity.Web.API.Application.Command;

public record GenerateCodeCommand(string Email, string MemoryKey,string GenerateCode) : IRequest<bool>;