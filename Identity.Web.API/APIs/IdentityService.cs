namespace Identity.Web.API.APIs;

public record IdentityService(IEmail Email, IUserRepository UserRepository,
    INotMediator NotMediator,IEventBus EventBus,INotDateTime NotDateTime,IdentityDomainToolServer IdentityDomainToolServer);