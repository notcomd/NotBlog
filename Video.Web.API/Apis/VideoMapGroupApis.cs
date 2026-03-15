using Microsoft.AspNetCore.Mvc;

namespace Video.Web.API.Apis;

public static class VideoMapGroupApis
{

    public static RouteGroupBuilder VideoMapGroup(this RouteGroupBuilder routeGroupBuilder)
    {
        var route=routeGroupBuilder.MapGroup("/Video");
        route.MapGet("/", HelloAsync);
        return route;
    }
    
    private static ActionResult<string> HelloAsync()
    {
        return "hello world";
    }
    
}