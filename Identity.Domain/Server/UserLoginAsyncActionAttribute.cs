using System.Security.Claims;
using Identity.Domain.IRepository;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace Identity.Domain.Server;

public class UserLoginAsyncActionAttribute : ActionFilterAttribute
{
    private readonly INotcomd_JwtTokenServer _jwtTokenServer;
    private readonly ILogger _logger;
    private readonly IOptionsSnapshot<Notcomd_JwtOptions> _optionsSnapshot;

    private readonly string _passwordHash;
    private readonly string _role;
    private readonly string _userEmail;
    private readonly IUserRepository _userRepository;

    public UserLoginAsyncActionAttribute(string passwordHash, string role, string userEmail, ILogger logger,
        INotcomd_JwtTokenServer notcomdJwtTokenServer
        , IOptionsSnapshot<Notcomd_JwtOptions> optionsSnapshot, IUserRepository userRepository)
    {
        (_passwordHash, _role, _userEmail, _logger, _jwtTokenServer, _optionsSnapshot, _userRepository) =
            (passwordHash, role, userEmail, logger, notcomdJwtTokenServer, optionsSnapshot, userRepository);
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var author = context.HttpContext.Response.Headers.Authorization;
        var data = _jwtTokenServer.JwtSecurityTokenHandlerAsync(_optionsSnapshot.Value.PrivateKey, author.ToString())
            .GetAwaiter().GetResult();
        var user = _userRepository.FindOneByUserAsync(data.Claims[ClaimTypes.Email].ToString()).GetAwaiter()
            .GetResult();
    }
}