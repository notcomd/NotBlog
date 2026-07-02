using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class IdentityApis
{
    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/Identity").WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/Register", Register).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/Login", Login).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/GenerateCode", GenerateCode).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="registerRequest">注册请求</param>
    /// <returns>注册结果</returns>
    private static async Task<IResult> Register([FromServices] IdentityService identityService,
        [FromBody] RegisterRequest registerRequest)
    {
        var userdata = await identityService.UserRepository.FindOneByUserAsync(registerRequest.UserEmail);

        if (userdata is not null)
        {
            return Results.BadRequest("用户已存在");
        }

        if (!string.IsNullOrWhiteSpace(registerRequest.VerificationCode))
        {
        }

        var command = new RegisterByUserCommand(registerRequest.UserPassword, registerRequest.VerificationCode,
            registerRequest.UserEmail, registerRequest.PhoneNumber);

        var registerIdentity = new IdentifiedCommand<RegisterByUserCommand, bool>(Guid.CreateVersion7(), command);

        var result = await identityService.NotMediator.SendAsync(registerIdentity);
        if (result)
        {
            return Results.Ok(new { message = "注册成功" });
        }
        else
        {
            return Results.BadRequest("null");
        }
    }


    private static async Task<IResult> Login([FromServices] IdentityService identityService,
        [FromBody] LoginRequest loginRequest)
    {
        return Results.Ok(new { message = "登录成功" });
    }


    private static async Task<IResult> GenerateCode([FromServices] IdentityService identityService,
        [FromBody] GenerateCodeRequest generateCodeRequest)
    {
        var commandGenerateCode = new GenerateCodeCommand(generateCodeRequest.Email);

        var identityCommand =
            new IdentifiedCommand<GenerateCodeCommand, string>(Guid.CreateVersion7(), commandGenerateCode);

        await identityService.NotMediator.SendAsync(identityCommand);

        return Results.Ok("");
    }


    private static async Task<IResult> ChangeByPassword([FromServices] IdentityService identityService,
        [FromBody] ChangeByPasswordRequest changeByPasswordRequestRequest)
    {
        var userdata = await identityService.UserRepository.FindOneByUserAsync(changeByPasswordRequestRequest.Email);
        if (userdata is null)
        {
            return Results.BadRequest("用户不存在");
        }

        if (string.Equals(changeByPasswordRequestRequest.Password, changeByPasswordRequestRequest.NewPassword))
            return Results.BadRequest("新密码不能与旧密码相同");

        var command = new ChangeByPasswordCommand(changeByPasswordRequestRequest.Email,
            changeByPasswordRequestRequest.NewPassword);

        var identityCommand =
            new IdentifiedCommand<ChangeByPasswordCommand, bool>(Guid.CreateVersion7(), command);


        var result = await identityService.NotMediator.SendAsync(identityCommand);

        if (result)
        {
            var emailCommand = new SendEmailCommand(changeByPasswordRequestRequest.Email, "重置账号消息！(≧∇≦)ﾉ",
                "你的账号密码重置了，请注意这是非常规的账号变动，确认为本人操作。");
            var identityEmailCommand = new IdentifiedCommand<SendEmailCommand, bool>(Guid.NewGuid(), emailCommand);
            await identityService.NotMediator.SendAsync(identityEmailCommand);
            return Results.Ok(new { message = "修改密码成功" });
        }
        else
        {
            return Results.BadRequest("修改密码失败");
        }
    }
}