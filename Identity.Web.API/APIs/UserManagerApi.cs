using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

public static class UserManagerApi
{
    public static RouteGroupBuilder MapUserManagerApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/user-manager").WithHttpLogging(HttpLoggingFields.All);

        route.MapGet("/GetUserInfo", GetUserInfo).WithHttpLogging(HttpLoggingFields.All);
        route.MapGet("/GetUserAllAsync", GetUserAllAsync).WithHttpLogging(HttpLoggingFields.All);
        return route;
    }


    public static async Task<IResult> GetUserInfo([FromServices] IdentityService identityService,
        [FromQuery] string userQuery)
    {
        // F-07：await 后返回真实用户数据（修复此前未 await 导致的 Task 序列化问题）
        var user = await identityService.UserService.GetUserInfoAsync(userQuery);
        return user is null ? Results.NotFound() : Results.Json(user);
    }

    public static async Task<IResult> GetUserAllAsync([FromServices] IdentityService identityService)
    {
        // F-07：返回全部用户（含 UserSafety / UserAccessFail 导航）
        var users = await identityService.UserService.FindUserByVagueAsync();
        return Results.Json(users);
    }
}
