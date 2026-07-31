using Identity.Domain.Entities.UserExternalLoginAggregate;
using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public sealed class CreateUserExternalLoginCommandHandler(
    IUserExternalLoginRepository userExternalLoginRepository,
    IUserRepository userRepository) : IRequestHandler<CreateUserExternalLoginCommand, bool>
{
    public async Task<bool> Handler(CreateUserExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var data = await userExternalLoginRepository
            .FindOneByUserIdAndProviderAsync(LoginProviderType.GitHub, request.ProviderKey);
        if (data is not null)
        {
            return false;
        }

        data = UserExternalLogin.Create(
            LoginProviderType.GitHub,
            request.ProviderKey,
            request.ProviderDisplayName,
            request.ProviderUnionId);
        data.UpdateTokens(
            request.ProviderAccessToken,
            request.ProviderRefreshToken, null);

        await userExternalLoginRepository.AddAsync(data);
        await userExternalLoginRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        return true;
    }
}

public class CreateUserExternalLoginIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<CreateUserExternalLoginCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<CreateUserExternalLoginCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}