using Identity.Domain.AggregatesModel.UserAggregate;
using Identity.Web.API.Application.Command;

using Microsoft.AspNetCore.Http.HttpResults;
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

        //route.MapPost("/TestSendEmail", TestSendEmailAsync).WithHttpLogging(HttpLoggingFields.All);

        route.MapGet("/TestGetHello", TestGetHelloAsync).WithHttpLogging(HttpLoggingFields.All);

        return route;
    }


    private static  string GetByTest(this HttpContext httpContext, [AsParameters]IdentityService identityService, CancellationToken cancellationToken)
    {
        var data = httpContext.User.Claims.Where(en => en.Issuer == "Role").FirstOrDefault();
        if (data is null)
        {
            return string.Empty;
        }
        return data.Value.ToLower();           
    }

    private async static Task<string> GetByTest2([AsParameters]IdentityService identityService, CreateByUserDto createByUserDto, CancellationToken cancellationToken)
    {
        var userbl = await identityService.UserRepository.FindOneByUserAsync(createByUserDto.Email);
        if (userbl is null)
            return string.Empty;
        return userbl!.ToString();

    }


    private static Task<string> GenerateCode([AsParameters]IdentityService identityService, GenerateCodeDto email, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //private async static Task<string> TestSendEmailAsync([AsParameters]IdentityService identityService, [EmailAddress(ErrorMessage = "格式错误")]string emailSendRecord, CancellationToken cancellationToken)
    //{
    //    if (emailSendRecord != string.Empty)
    //    {
    //        await identityService.NotMediator.SendAsync(new CreateByEmailUserCommand(emailSendRecord, "123456", "User","user"), cancellationToken);
    //        return "发送成功";
    //    }
    //    return "发送失败，请检查邮箱格式";
    //}

    private static Task<IActionResult> TestGetHelloAsync()
    {
        return Task.FromResult<IActionResult>(IdentityResult<string>.Result("hello world!", EnumStatusCode.Ok, string.Empty));
    }

    private record GenerateCodeDto(string Email);

    private record CreateByUserDto(string Email, string Password, string Code);
}