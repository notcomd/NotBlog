namespace FileDev.Web.API.Middleware;

using System.Security.Claims;

public class FileAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FileAccessMiddleware> _logger;

    public FileAccessMiddleware(RequestDelegate next, ILogger<FileAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, INotFileService notFileService)
    {
        // Only check file download/access routes
        if (context.Request.Path.StartsWithSegments("/api/filestorage/download") ||
            context.Request.Path.StartsWithSegments("/api/filestorage/file"))
        {
            // Extract fileId from route
            var fileIdStr = context.Request.RouteValues["fileId"]?.ToString()
                ?? context.Request.Query["fileId"].ToString();

            if (Guid.TryParse(fileIdStr, out var fileId))
            {
                var userIdClaim = context.User.FindFirst("id")
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var file = await notFileService.GetFileByIdAsync(fileId);
                    if (file != null && file.FileIdentity == FileIdentity.FilePrivate && file.UserId != userId)
                    {
                        _logger.LogWarning("用户 {UserId} 尝试访问未授权的文件 {FileId}", userId, fileId);
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new { error = "无权访问此文件" });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}

public static class FileAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseFileAccess(this IApplicationBuilder builder)
        => builder.UseMiddleware<FileAccessMiddleware>();
}
