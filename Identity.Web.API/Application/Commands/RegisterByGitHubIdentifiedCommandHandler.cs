using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class RegisterByGitHubIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<RegisterByGitHubCommand, RegisterByGitHubResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<RegisterByGitHubCommand, RegisterByGitHubResult>(logger, mediator, requestManagement)
{
    protected override RegisterByGitHubResult CreateResultForDuplicateRequest()
    {
        return new RegisterByGitHubResult(false,
            new Notcomd.Token.JWT.Core.TokenResult { AccessToken = string.Empty, TokenType = "Bearer" },
            string.Empty, string.Empty, null, null);
    }
}
