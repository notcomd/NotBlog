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

        route.MapPost("/TestSendEmail", TestSendEmailAsync).WithHttpLogging(HttpLoggingFields.All);

        route.MapGet("/TestGetHello", TestGetHelloAsync).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }


    private static Task<IActionResult> GetByTest(this HttpContext httpContext, [AsParameters]IdentityService identityService, CancellationToken cancellationToken)
    {
        var data = httpContext.User.Claims.Where(en => en.Issuer == "Role").FirstOrDefault();
        if (data is null)
            return Task.FromResult<IActionResult>(new ResultNotIdentity<string>($"Error", StatusCode.Error, $"Error"));

        return Task.FromResult<IActionResult>(new ResultNotIdentity<string>("hello world!", StatusCode.Ok, string.Empty));
    }

    private async static Task<IActionResult> GetByTest2([AsParameters]IdentityService identityService, CreateByUserDto createByUserDto, CancellationToken cancellationToken)
    {
        var userbl = await identityService.UserRepository.FindOneByUserAsync(createByUserDto.Email);
        if (userbl is null)
            return new ResultNotIdentity<User>($"错误", StatusCode.Error, userbl);
        return new ResultNotIdentity<User>($"成功", StatusCode.Ok, userbl);
    }


    private static Task<string> GenerateCode([AsParameters]IdentityService identityService, GenerateCodeDto email, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async static Task<IActionResult> TestSendEmailAsync([AsParameters]IdentityService identityService, [EmailAddress(ErrorMessage = "格式错误")]string emailSendRecord, CancellationToken cancellationToken)
    {
        if (emailSendRecord != string.Empty)
        {
            await identityService.NotMediator.SendAsync(new CreateByUserCommand(emailSendRecord, "123456", "123456"), cancellationToken);
            return new ResultNotIdentity<string>("发送成功", StatusCode.Ok, emailSendRecord);
        }
        return new ResultNotIdentity<string>("发送失败", StatusCode.Error, emailSendRecord);
    }

    private static Task<IActionResult> TestGetHelloAsync()
    {
        return Task.FromResult<IActionResult>(new ResultNotIdentity<string>("hello world!", StatusCode.Ok, string.Empty));
    }

    private record GenerateCodeDto(string Email);

    private record CreateByUserDto(string Email, string Password, string Code);
}