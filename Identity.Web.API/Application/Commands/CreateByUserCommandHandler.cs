using Identity.Infrastructure.EntityFramework;
using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class CreateUserCommandHandler(
    ILogger<CreateUserCommandHandler> logger,
    IEmailCodeSend emailCodeSend,
    IdentityDbContext context,
    IUserRepository userRepository)
    : IRequestHandler<CreateUserCommand, bool>
{
    private readonly IdentityDbContext _context = context ??
                                                  throw new ArgumentNullException(nameof(context));

    private readonly IEmailCodeSend _emailCodeSend =
        emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));

    private readonly ILogger<CreateUserCommandHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IUserRepository _userRepository =
        userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<bool> Handler(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await _emailCodeSend.SendEmailCodeAsync(request.Email, request.Code);
        _logger.LogInformation($"[{DateTime.UtcNow}]Email Send! ");
        return true;
    }

    public class CreateUserIdentifiedCommandHandler<T>(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<IdentifiedCommandHandler<CreateUserCommand, bool>> logger)
        : IdentifiedCommandHandler<CreateUserCommand, bool>(logger, mediator, requestManagement)
    {
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}