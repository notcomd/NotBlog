using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class RegisterByUserCommandHandler(
    ILogger<RegisterByUserCommandHandler> logger,
    IUserRepository userRepository
)
    : NotMediator.IRequestHandler<RegisterByUserCommand, bool>
{
    public async Task<bool> Handler(RegisterByUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindOneByUserAsync(command.UserEmail ??
                                                           throw new ArgumentNullException(nameof(command.UserEmail)));
        if (user is not null)
        {
            return false;
        }

        user = await User.CreateByEmailUser(
            Guid.NewGuid(),
            command.UserEmail,
            command.PasswordHash,
            null,
            null);

        await userRepository.AddOneByUserAsync(user);
        await userRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
        return true;
    }
}

public class RegisterByUserIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<RegisterByUserCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<RegisterByUserCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}