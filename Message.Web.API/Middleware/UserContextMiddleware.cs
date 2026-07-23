namespace Message.Web.API.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var rolesHeader = context.Request.Headers["X-User-Roles"].FirstOrDefault();

        if (Guid.TryParse(userIdHeader, out var userGuid))
        {
            var roles = rolesHeader?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       ?? Array.Empty<string>();
            currentUser.SetUser(userGuid, roles);
        }

        await _next(context);
    }
}

public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
