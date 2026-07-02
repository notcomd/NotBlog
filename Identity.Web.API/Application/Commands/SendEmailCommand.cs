namespace Identity.Web.API.Application.Commands;

public record SendEmailCommand(string ToEmail, string Subject, string Body) : IRequest<bool>;