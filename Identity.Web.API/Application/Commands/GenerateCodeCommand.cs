namespace Identity.Web.API.Application.Commands;

public record GenerateCodeCommand(string Email) : IRequest<string>;