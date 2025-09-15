namespace Identity.Web.API.APIs;

public sealed record IdentityService(IEmail Email, IUserRepository UserRepository,
<<<<<<< HEAD
    INotMediator NotMediator, IEventBus EventBus, INotDateTime NotDateTime,
    IdentityDomainToolServer IdentityDomainToolServer
    , IdentityDomainCheckLogInServer IdentityDomainCheckLogInServer,
    IdentityDomainUserManagerServer IdentityDomainUserManagerServer,
    IdentityDomainRoleManagerServer IdentityDomainRoleManagerServer,
=======
    INotMediator NotMediator,IEventBus EventBus,INotDateTime NotDateTime,
    IdentityDomainToolServer IdentityDomainToolServer
    ,IdentityDomainCheckLogInServer IdentityDomainCheckLogInServer,
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
    IUserRoleRepository UserRoleRepository);