using Identity.Domain.Entities.UserAggregate;
using Identity.Domain.Result;
using Identity.Web.API.Application.Commands;
using Microsoft.AspNetCore.HttpLogging;

namespace Identity.Web.API.APIs;

public static class NotMapIdentityApis
{
    public static RouteGroupBuilder NotMapIdentityApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/Identity").WithHttpLogging(HttpLoggingFields.All);

        route.MapGet("/", GetByTest).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/GenerateCode", GenerateCode).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/CreateByUser", GetByTest2).WithHttpLogging(HttpLoggingFields.All);

        route.MapPost("/TestSendEmail", TestSendEmailAsync).WithHttpLogging(HttpLoggingFields.All);

        route.MapGet("/TestGetHello", TestGetHelloAsync).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }


    private static Task<IActionResult> GetByTest(this HttpContext httpContext,
        [AsParameters] IdentityService identityService, CancellationToken cancellationToken)
    {
        var data = httpContext.User.Claims.Where(en => en.Issuer == "Role").FirstOrDefault();
        if (data is null)
            return Task.FromResult<IActionResult>(IdentityResult<string>.NotAuthorized("权限不足", $"{DateTime.Now}"));

        return Task.FromResult<IActionResult>(IdentityResult<string>.Success("hello world!", $"{DateTime.Now}"));
    }


    private static Task<IActionResult> Login([AsParameters] IdentityService identityService,
        [EmailAddress(ErrorMessage = "格式错误")] string email, string password, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    private static async Task<IActionResult> GetByTest2([AsParameters] IdentityService identityService,
        CreateByUserDto createByUserDto, CancellationToken cancellationToken)
    {
        var userbl = await identityService.UserRepository.FindOneByUserAsync(createByUserDto.Email);
        if (userbl is null)
            return IdentityResult<User>.Error("错误", userbl);
        return IdentityResult<User>.Success("成功", userbl);
    }


    private static Task<string> GenerateCode([AsParameters] IdentityService identityService, GenerateCodeDto email,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static async Task<IActionResult> TestSendEmailAsync([AsParameters] IdentityService identityService,
        [EmailAddress(ErrorMessage = "格式错误")] string emailSendRecord, CancellationToken cancellationToken)
    {
        if (emailSendRecord != string.Empty)
        {
            await identityService.NotMediator.SendAsync(new CreateUserCommand(emailSendRecord, "123456", "123456"),
                cancellationToken);
            return IdentityResult<string>.Success("发送成功", $"{DateTime.Now}");
        }

        return IdentityResult<string>.Error(emailSendRecord, emailSendRecord);
    }

    private static Task<IActionResult> TestGetHelloAsync()
    {
        return Task.FromResult<IActionResult>(IdentityResult<string>.Other("hello world!", StatusCode.Ok,
            string.Empty));
    }

    private record GenerateCodeDto(string Email);

    private record CreateByUserDto(string Email, string Password, string Code);
}