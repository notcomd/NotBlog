using Identity.Web.API.Application.Command;
using Identity.Web.API.Application.Models;

using Microsoft.AspNetCore.HttpLogging;

namespace Identity.Web.API.APIs;

public static class NotMapIdentityApis
{
    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {

        var route = routeBuilder.MapGroup(("/Identity")).WithHttpLogging(HttpLoggingFields.All);


        route.MapGet("/", GetByTest).WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/GenerateCode", GenerateCode).WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/CreateByUser", GetByTest2).WithHttpLogging(HttpLoggingFields.All);


        route.MapGet("/TestGetHello", TestGetHelloAsync).WithHttpLogging(HttpLoggingFields.All);


        route.MapPost("/SignUpWithEmail", SignUpWithEmailAsync).WithHttpLogging(HttpLoggingFields.All);


        return route;
    }


    private static string GetByTest(this HttpContext httpContext, [AsParameters] IdentityService identityService, CancellationToken cancellationToken)
    {
        var data = httpContext.User.Claims.Where(en => en.Issuer == "Role").FirstOrDefault();
        if (data is null)
        {
            return string.Empty;
        }
        return data.Value.ToLower();
    }

    private async static Task<string> GetByTest2([AsParameters] IdentityService identityService, CreateByUserDto createByUserDto, CancellationToken cancellationToken)
    {
        var userbl = await identityService.UserRepository.FindOneByUserAsync(createByUserDto.Email);
        if (userbl is null)
            return string.Empty;
        return userbl!.ToString();

    }


    private static async Task GenerateCode([AsParameters] IdentityService identityService, GenerateCodeDto email, CancellationToken cancellationToken)
    {
        var code = await identityService.NotMediator.SendAsync(new GenerateCodeCommand(email.Email, 9), cancellationToken);
        await identityService.NotMediator.SendAsync(new SendWithEmailCommand(email.Email, code), cancellationToken);
    }


    private static async Task SignUpWithEmailAsync([AsParameters] IdentityService identityService, RquistSignUpWithEmailModel createByUserDto, CancellationToken cancellationToken)
    {
        if (createByUserDto is null) throw new ArgumentNullException(nameof(createByUserDto));
        var signal = await identityService.NotMediator.SendAsync(new CreateByEmailUserCommand(createByUserDto.SignUpEmail,
             createByUserDto.HashPassword, "User", "User", identityService.NotDateTime.UtcNow), cancellationToken);
        var status = await identityService.IdentityDomainToolServer.IsCheckWithAlreadyExistsAsync(createByUserDto.SignUpEmail);
              
    }



    private static Task<IActionResult> TestGetHelloAsync()
    {
        return Task.FromResult<IActionResult>(IdentityResult<string>.Result("hello world!", EnumStatusCode.Ok, string.Empty));
    }

    private record GenerateCodeDto(string Email);

    private record CreateByUserDto(string Email, string Password, string Code);
}