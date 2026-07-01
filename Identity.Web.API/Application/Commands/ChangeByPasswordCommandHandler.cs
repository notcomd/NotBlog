using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class ChangeByPasswordCommandHandler(IUserRepository userRepository)
    : NotMediator.IRequestHandler<ChangeByPasswordCommand, bool>
{
    public async Task<bool> Handler(ChangeByPasswordCommand request, CancellationToken cancellationToken)
    {
        var data = await userRepository.FindOneByUserAsync(request.Email);

        if (data is null)
        {
            return false;
        }

        await data.ChangeByPasswordAsync(request.NewPasswordHash);
        await userRepository.UpdateByUserAsync(data);
        await userRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
        return true;
    }
}

public class ChangeByPasswordIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<ChangeByPasswordCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<ChangeByPasswordCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}