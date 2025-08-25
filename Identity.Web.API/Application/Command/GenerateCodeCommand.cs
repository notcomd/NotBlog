namespace Identity.Web.API.Application.Command;

public record GenerateCodeCommand(string Email, int LenghtGenerate) : IRequest<string>;