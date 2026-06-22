using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class NotMapIdentityApis
{
    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/Identity").WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/Register", Register).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="registerRequest">注册请求</param>
    /// <returns>注册结果</returns>
    public static async Task<IResult> Register([FromServices] IdentityService identityService,
        [FromBody] RegisterRequest registerRequest)
    {
        var command = new RegisterByUserCommand(registerRequest.UserPassword, registerRequest.VerificationCode,
            registerRequest.UserEmail, registerRequest.PhoneNumber);
        var result = await identityService.NotMediator.SendAsync(command);
        if (result)
        {
            return Results.Ok(new { message = "注册成功" });
        }
        else
        {
            return Results.BadRequest("null");
        }
    }
}