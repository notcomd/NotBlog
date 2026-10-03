namespace Identity.Web.API.DependencyInjection;

/// <summary>
/// Identity API 端点使用的服务聚合（供 <c>[FromServices]</c> 一次性注入多个仓储/服务/中介者）。
/// </summary>
public record IdentityServiceDi(
    IEmailCodeSend EmailCodeSend,
    IUserRepository UserRepository,
    INotMediator NotMediator,
    ILogger<IdentityServiceDi> Logger,
    IUserService UserService,
    IUserRoleRepository UserRoleRepository);