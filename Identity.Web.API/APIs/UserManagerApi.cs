using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class UserManagerApi
{
    public static RouteGroupBuilder MapUserManagerApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/UserManager").WithHttpLogging(HttpLoggingFields.All);


        return route;
    }


    public static Task<IResult> GetUserInfo([FromServices] IdentityService identityService,
        [FromQuery] string userQuery)
    {
        return Task.FromResult<IResult>(Results.Json(identityService.UserService.GetUserInfo(userQuery)));
    }

    public static Task<IResult> GetUserAllAsync([FromServices] IdentityService identityService)
    {
        return Task.FromResult<IResult>(Results.Json(identityService.UserService.GetUserAll()));
    }
}