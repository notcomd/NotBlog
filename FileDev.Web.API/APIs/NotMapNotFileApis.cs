namespace FileDev.Web.API.APIs;

public static class NotMapNotFileApis
{
    public static RouteGroupBuilder NotFileRouterGroup(this RouteGroupBuilder routeGroupBuilder)
    {
        var route= routeGroupBuilder.WithName("NotFileApi");
        route.MapGet("/hello", TestHello);
        
        return routeGroupBuilder;
    }

    private static Task<string> TestHello(this HttpContext httpContext)
    {
        return Task.FromResult("Hello World");
    }
    
}