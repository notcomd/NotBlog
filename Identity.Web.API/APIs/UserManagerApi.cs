using Microsoft.AspNetCore.HttpLogging;

namespace Identity.Web.API.APIs;

public static class UserManagerApi
{
    public static RouteGroupBuilder MapUserManagerApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/UserManager").WithHttpLogging(HttpLoggingFields.All);


        return route;
    }
}