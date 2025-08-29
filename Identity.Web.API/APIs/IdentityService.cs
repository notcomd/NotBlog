namespace Identity.Web.API.APIs;

public sealed record IdentityService(IEmail Email, IUserRepository UserRepository,
    INotMediator NotMediator, IEventBus EventBus, INotDateTime NotDateTime,
    IdentityDomainToolServer IdentityDomainToolServer
    , IdentityDomainCheckLogInServer IdentityDomainCheckLogInServer,
    IdentityDomainUserManagerServer IdentityDomainUserManagerServer,
    IdentityDomainRoleManagerServer IdentityDomainRoleManagerServer,
    IUserRoleRepository UserRoleRepository);