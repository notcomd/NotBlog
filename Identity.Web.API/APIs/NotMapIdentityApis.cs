using System.Security.Claims;

using Identity.Web.API.Application.Command;
using Identity.Web.API.Application.Models;

using Microsoft.AspNetCore.HttpLogging;

namespace Identity.Web.API.APIs;

public static class NotMapIdentityApis
{
    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {

        var route = routeBuilder.MapGroup(("/NotAuthor"))
            .WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/GetRquestGenerateCode", RegisterWithGenerateCodeAsync)
            .WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/RegisterWithEmailUser", RegisterWithEmailAsync)
            .WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/LogInWithEmail", LogInWithEmailAsync)
            .WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/LogInWithGenerateCode", LogInWithGenerateCodeAsync)
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/LogInByEmailWithGenerateCode", LogInByEmailWithGenerateCodeAsync)
            .WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/TestRegister", TestRegisterWithEmailAsync)
            .WithDisplayName("测试注册接口")
            .WithDescription("测试注册接口");

        return route;
    }

    private static async Task<IdentityResult<string>> RegisterWithEmailAsync([AsParameters] IdentityService identityService, RquestRegisterWithEmailModel rquestRegisterWithEmail, CancellationToken cancellationToken)
    {

        if (rquestRegisterWithEmail is null) throw new ArgumentNullException(nameof(rquestRegisterWithEmail));

        if (await identityService.IdentityDomainToolServer.IsCheckWithVerifyGenerateCodeAsync(rquestRegisterWithEmail.RegisterEmail, rquestRegisterWithEmail.GenerateCode))
        {
            var signal = await identityService.NotMediator.SendAsync(new CreateByEmailUserCommand(rquestRegisterWithEmail.RegisterEmail,
             rquestRegisterWithEmail.HashPassword, "User", "User", identityService.NotDateTime.UtcNow), cancellationToken);
            return signal
                ? await IdentityResult<string>.ResultAsync("注册成功，可以登录啦！", EnumStatusCode.Ok, "操作成功")
                : await IdentityResult<string>.ResultAsync("注册失败，用户可能已存在", EnumStatusCode.Error, "操作失败");
        }
        return await IdentityResult<string>.ResultAsync("注册失败，验证码错误或已过期", EnumStatusCode.Error, "操作失败");
    }


    private static async Task<IdentityResult<string>> RegisterWithGenerateCodeAsync([AsParameters] IdentityService identityService, RequestRegisterWithGenerateeCodeModel requestRegisterWithGenerateeCodeModel, CancellationToken cancellationToken)
    {
        if (requestRegisterWithGenerateeCodeModel is null) throw new ArgumentNullException(nameof(requestRegisterWithGenerateeCodeModel));
        if (await identityService.IdentityDomainToolServer.IsCheckWithAlreadyExistsAsync(requestRegisterWithGenerateeCodeModel.RegisterEmail))
        {
            return await IdentityResult<string>.ResultAsync($"验证码以生成，在生效期间无法再次生成。", EnumStatusCode.Error, "无法完成操作");
        }

        var code = await identityService.NotMediator.SendAsync(new GenerateCodeCommand(requestRegisterWithGenerateeCodeModel.RegisterEmail, 8));
        await identityService.NotMediator.SendAsync(new SendWithEmailCommand("Hi~,这是一封重要的邮件请查收(｡･∀･)ﾉﾞ嗨", requestRegisterWithGenerateeCodeModel.RegisterEmail, code), cancellationToken);
        return await IdentityResult<string>.ResultAsync($"验证码已发送到{requestRegisterWithGenerateeCodeModel.RegisterEmail}，注意查收（*＾-＾*）", EnumStatusCode.Ok, "操作成功");
    }


    private static async ValueTask<IdentityResult<string>> LogInWithEmailAsync([AsParameters] IdentityService identityService, RequestLogInWithEmailModel requestLogInWithEmailModel)
    {
        ArgumentNullException.ThrowIfNull(requestLogInWithEmailModel);
        if (await identityService.IdentityDomainCheckLogInServer.CheckLogInWhitEmailAsync(requestLogInWithEmailModel.LoginEmail, requestLogInWithEmailModel.HashPassword) is Domain.IdentiyResult.UserAccessResult.Success
            && await identityService.IdentityDomainToolServer.IsCheckWithVerifyGenerateCodeAsync(requestLogInWithEmailModel.LoginEmail, requestLogInWithEmailModel.GenerateCode))
        {
            var user = await identityService.UserRepository.FindOneByUserAsync(requestLogInWithEmailModel.LoginEmail);
            if (user is null)
                throw new InvalidOperationException("User not found.");
            var role = await identityService.UserRoleRepository.FindByUserRoleAsync(user.UserRoleGuid);
            if (role is null)
                throw new InvalidOperationException("Role not found.");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, requestLogInWithEmailModel.LoginEmail),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, role.RoleName)
            };
            var token = await identityService.IdentityDomainToolServer.BuilderWithAuthorToknAsync(claims);
            return await IdentityResult<string>.ResultAsync(token, EnumStatusCode.Ok, "操作成功");
        }
        return await IdentityResult<string>.ResultAsync("登录失败", EnumStatusCode.Error, "操作失败");
    }




    private static async ValueTask<IdentityResult<string>> LogInWithGenerateCodeAsync([AsParameters] IdentityService identityService, RequestGetLogInGenerateCodeModel requestGetLogInGenerateCodeModel)
    {
        //if (requestGetLogInGenerateCodeModel is null)
        //    throw new ArgumentNullException(nameof(requestGetLogInGenerateCodeModel));
        //if (await identityService.IdentityDomainCheckLogInServer.CheckLogInWhitEmailAsync(requestLoginWithGenerateCodeModel.LoginEmail, requestLoginWithGenerateCodeModel.HashPassword) is Domain.IdentiyResult.UserAccessResult.Success)
        //{
        //    var code = await identityService.NotMediator.SendAsync(new GenerateCodeCommand(requestLoginWithGenerateCodeModel.LoginEmail, 8));
        //    await identityService.NotMediator.SendAsync(new SendWithEmailCommand("Hi~,这是一封重要的邮件请查收(｡･∀･)ﾉﾞ嗨", requestLoginWithGenerateCodeModel.LoginEmail, code));
        //    await identityService.UserRepository.UnitOfWork.SavaChangesAsync();
        //    return await IdentityResult<string>.ResultAsync($"验证码已发送{requestLoginWithGenerateCodeModel.LoginEmail}", EnumStatusCode.Ok, "操作成功");
        //}
        //await identityService.UserRepository.UnitOfWork.SavaChangesAsync();
        //return await IdentityResult<string>.ResultAsync("登录失败", EnumStatusCode.Error, "操作失败");
        if (requestGetLogInGenerateCodeModel is null)
            return await IdentityResult<string>.ResultAsync("请求参数不能为空", EnumStatusCode.Error, "操作失败");
        if (await identityService.IdentityDomainToolServer.IsCheckWithAlreadyExistsAsync(requestGetLogInGenerateCodeModel.LoginByEmailOrPhone))
        {
            return await IdentityResult<string>.ResultAsync("验证码已发送，不要多次请求！", EnumStatusCode.Reset, "异常操作");
        }
        var code = await identityService.NotMediator.SendAsync(new GenerateCodeCommand(requestGetLogInGenerateCodeModel.LoginByEmailOrPhone, 8));
        await identityService.NotMediator.SendAsync(new SendWithEmailCommand("Hi~,这是一封重要的邮件请查收(｡･∀･)ﾉﾞ嗨", requestGetLogInGenerateCodeModel.LoginByEmailOrPhone, code));
        return await IdentityResult<string>.ResultAsync("验证码以发送", EnumStatusCode.Ok, "操作成功");
    }


    private static async ValueTask<IdentityResult<string>> LogInByEmailWithGenerateCodeAsync([AsParameters] IdentityService identityService, RequestLogInWithEmailModel requestLogInWithEmailModel)
    {
        ArgumentNullException.ThrowIfNull(requestLogInWithEmailModel);
        if (await identityService.IdentityDomainToolServer.IsCheckWithVerifyGenerateCodeAsync(requestLogInWithEmailModel.LoginEmail, requestLogInWithEmailModel.GenerateCode))
        {
            var user = await identityService.UserRepository.FindOneByUserAsync(requestLogInWithEmailModel.LoginEmail);
            if (user is null)
                throw new InvalidOperationException("User not found.");
            var role = await identityService.UserRoleRepository.FindByUserRoleAsync(user.UserRoleGuid);
            if (role is null)
                throw new InvalidOperationException("Role not found.");
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, requestLogInWithEmailModel.LoginEmail),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, role.RoleName)
            };
            var token = await identityService.IdentityDomainToolServer.BuilderWithAuthorToknAsync(claims);
            return await IdentityResult<string>.ResultAsync(token, EnumStatusCode.Ok, "操作成功");
        }
        return await IdentityResult<string>.ResultAsync("登录失败", EnumStatusCode.Error, "操作失败");
    }



    private static async Task<IdentityResult<string>> TestRegisterWithEmailAsync([AsParameters] IdentityService identityService, RquestRegisterWithEmailModel rquestRegisterWithEmail, CancellationToken cancellationToken)
    {

        if (rquestRegisterWithEmail is not null)
        {
            var signal = await identityService.NotMediator.SendAsync(new CreateByEmailUserCommand(rquestRegisterWithEmail.RegisterEmail,
             rquestRegisterWithEmail.HashPassword, "User", "User", identityService.NotDateTime.UtcNow), cancellationToken);
            return signal
                ? await IdentityResult<string>.ResultAsync("注册成功，可以登录啦！", EnumStatusCode.Ok, "操作成功")
                : await IdentityResult<string>.ResultAsync("注册失败，用户可能已存在", EnumStatusCode.Error, "操作失败");
        }
        return await IdentityResult<string>.ResultAsync("注册失败，验证码错误或已过期", EnumStatusCode.Error, "操作失败");
    }





}