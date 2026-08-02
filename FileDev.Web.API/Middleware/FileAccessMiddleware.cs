namespace FileDev.Web.API.Middleware;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FileDev.Domain.IServices;

/// <summary>
/// 基于 <see cref="FileAccessAttribute"/> 标记的文件访问权限中间件。
/// 不再硬编码路径，而是通过 Endpoint Metadata 检测标记的端点，
/// 根据 Attribute 配置的策略自动提取文件ID并执行访问控制。
///
/// 使用方式：
///   app.UseRouting();
///   app.UseAuthentication();
///   app.UseAuthorization();
///   app.UseFileAccess();  // 在 UseRouting / UseAuth 之后
///   app.MapControllers();
///
/// 端点标记示例：
///   [FileAccess]                                    // 默认：Route 参数 "fileId"，OwnerOnly
///   [FileAccess("id")]                              // 自定义参数名
///   [FileAccess(Policy = FileAccessPolicy.AuthenticatedOnly)]  // 仅验证登录
///   [FileAccess("fileId", Source = FileIdSource.Query)]        // 从查询字符串提取
/// </summary>
public class FileAccessMiddleware : IMiddleware
{
    private readonly ILogger<FileAccessMiddleware> _logger;

    private readonly INotFileService _notFileService;

    public FileAccessMiddleware(ILogger<FileAccessMiddleware> logger, INotFileService notFileService)
    {
        _logger = logger;
        _notFileService = notFileService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        var attr = endpoint?.Metadata.GetMetadata<FileAccessAttribute>();

        if (attr is not null)
        {
            var fileId = ExtractFileId(context, attr);
            var userId = ExtractUserId(context);

            if (fileId is null)
            {
                _logger.LogWarning("[FileAccess] 无法从请求中提取文件ID (Param={Param}, Source={Source})",
                    attr.FileIdParamName, attr.Source);
                await Forbidden(context, "缺少文件ID参数");
                return;
            }

            if (userId is null)
            {
                _logger.LogWarning("[FileAccess] 无法从请求中提取用户ID");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "请先登录" });
                return;
            }

            if (!await CheckAccessAsync(_notFileService, fileId.Value, userId.Value, attr, context))
                return;
        }

        await next(context);
    }

    // ══════════════════════════════════════════════════
    //  Private helpers
    // ══════════════════════════════════════════════════

    /// <summary>根据 Attribute 配置从请求中提取文件ID</summary>
    private static Guid? ExtractFileId(HttpContext context, FileAccessAttribute attr)
    {
        var raw = attr.Source switch
        {
            FileIdSource.Route => context.Request.RouteValues[attr.FileIdParamName]?.ToString()
                ?? context.Request.Query[attr.FileIdParamName].ToString(),
            FileIdSource.Query => context.Request.Query[attr.FileIdParamName].ToString(),
            _ => null
        };

        return Guid.TryParse(raw, out var fid) ? fid : null;
    }

    /// <summary>从 Claims 提取当前用户ID</summary>
    private static Guid? ExtractUserId(HttpContext context)
    {
        var claim = context.User.FindFirst("id")
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

        return claim is not null && Guid.TryParse(claim.Value, out var uid) ? uid : null;
    }

    /// <summary>根据策略执行访问检查</summary>
    private async Task<bool> CheckAccessAsync(
        INotFileService notFileService,
        Guid fileId,
        Guid userId,
        FileAccessAttribute attr,
        HttpContext context)
    {
        switch (attr.Policy)
        {
            case FileAccessPolicy.OwnerOnly:
            {
                var file = await notFileService.GetFileByIdAsync(fileId);
                if (file is null)
                {
                    _logger.LogWarning("[FileAccess] 文件不存在: FileId={FileId}", fileId);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { error = "文件不存在" });
                    return false;
                }

                if (file.UserId != userId)
                {
                    _logger.LogWarning("[FileAccess] 用户无权访问: UserId={UserId}, FileId={FileId}, Owner={OwnerId}",
                        userId, fileId, file.UserId);
                    await Forbidden(context, "无权访问此文件");
                    return false;
                }

                return true;
            }

            case FileAccessPolicy.AuthenticatedOnly:
                // 已认证即可（前面已检查 userId != null）
                return true;

            default:
                _logger.LogError("[FileAccess] 未知的访问策略: {Policy}", attr.Policy);
                await Forbidden(context, "访问被拒绝");
                return false;
        }
    }

    private static async Task Forbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}

public static class FileAccessMiddlewareExtensions
{
    public static IApplicationBuilder UseFileAccess(this IApplicationBuilder builder)
        => builder.UseMiddleware<FileAccessMiddleware>();
}
