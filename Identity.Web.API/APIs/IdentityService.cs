namespace Identity.Web.API.APIs;

public record IdentityService(IEmail Email, UserRepositoryServer UserRepositoryServer, IUserRepository UserRepository, INotMediator NotMediator);