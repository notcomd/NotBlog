
namespace Message.Web.API.Middleware;
public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
