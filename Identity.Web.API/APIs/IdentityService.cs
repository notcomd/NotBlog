namespace Identity.Web.API.APIs;

public record IdentityService(
    IEmailCodeSend EmailCodeSend,
    IUserRepository UserRepository,
    INotMediator NotMediator,
    ILogger<IdentityService> Logger,
    IUserService UserService);