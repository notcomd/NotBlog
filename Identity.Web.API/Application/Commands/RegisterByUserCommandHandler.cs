using Identity.Infrastructure.Idempotent;
using Identity.Web.API.Application.IntegrationEvents.Events;
using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.ICache;
using CacheMemory.Core;
namespace Identity.Web.API.Application.Commands;

public class RegisterByUserCommandHandler(
    ILogger<RegisterByUserCommandHandler> logger,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IIdentityCacheService identityCacheService,
    IEventBus eventBus
)
    : NotMediator.IRequestHandler<RegisterByUserCommand, bool>
{

    //private readonly string cacheKey = "RegisterByUserCommandHandler";



    public async Task<bool> Handler(RegisterByUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindOneByUserAsync(command.UserEmail ?? throw new ArgumentNullException(nameof(command.UserEmail)));
        if (user is not null)
        {
            return false;
        }
        var userRole = await userRoleRepository.FindByUserRoleAsync("User");
        if (userRole is null)
        {
            throw new ArgumentNullException(nameof(userRole));
        }
        var cacheData = await identityCacheService.GetStringAsync($"Login_{command.UserEmail}", cancellationToken);
        if (cacheData is null)
        {
            return false;
        }
        if (cacheData != command.Code)
        {
            return false;
        }
        await identityCacheService.RemoveAsync($"Login_{command.UserEmail}", cancellationToken);

        user = await User.CreateByEmailUser(
            userRole.RoleGuid,
            command.UserEmail,
            command.PasswordHash,
            null,
            null);
        await userRepository.AddOneByUserAsync(user);
        await userRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
        await eventBus.PublishAsync(new RegisterByUserIntegrationEvent(user.UserGuid));
        logger.LogInformation("[RegisterByUserCommandHandler] 注册用户成功: UserId={UserId}",
            user.UserGuid);
        await Task.CompletedTask;
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