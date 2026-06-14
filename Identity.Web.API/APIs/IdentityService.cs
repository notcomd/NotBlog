namespace Identity.Web.API.APIs;

public record IdentityService(IEmailSender Email, IUserRepository UserRepository, INotMediator NotMediator);