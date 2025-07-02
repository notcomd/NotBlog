namespace Identity.Web.API.Application.Command;

public record GenerateCodeCommand(string Email) : IRequest<string>;