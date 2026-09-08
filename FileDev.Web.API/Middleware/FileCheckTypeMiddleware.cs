using Commons.Result;
using FileDev.Domain.Options;
using Microsoft.AspNetCore.Http.Features;

namespace FileDev.Web.API.Middleware;

/// <summary>
/// 验证上传文件类型是否在服务允许范围内。
/// 白名单由 <see cref="NotFileStorageOptions.AllowedExtensions"/> 配置驱动，
/// 可在 appsettings.json 的 NotFileStorage 节中动态调整。
/// </summary>
public class FileCheckTypeMiddleware : IMiddleware
{
    private readonly IOptionsSnapshot<NotFileStorageOptions> _options;
    private readonly ILogger<FileCheckTypeMiddleware> _logger;

    public FileCheckTypeMiddleware(
        IOptionsSnapshot<NotFileStorageOptions> options,
        ILogger<FileCheckTypeMiddleware> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 仅拦截上传类请求
        if (!IsUploadRequest(context))
        {
            _logger.LogDebug("[FileCheck] 跳过 — 非上传请求: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await next(context);
            return;
        }

        _logger.LogInformation("[FileCheck] 开始校验: {Method} {Path}, ContentType={ContentType}",
            context.Request.Method, context.Request.Path, context.Request.ContentType);

        var form = await TryReadFormAsync(context);
        if (form is null)
        {
            // Major：原代码在表单解析失败/无 Endpoint 时直接放行，存在安全风险（攻击者构造异常
            // Content-Type 绕过白名单）。改为返回 400 拒绝，确保所有 multipart 上传必经白名单校验。
            _logger.LogWarning("[FileCheck] 拒绝 — 表单解析失败或无 Endpoint: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(ApiResponseResult.Failure("上传表单解析失败", 400));
            return;
        }

        _logger.LogInformation("[FileCheck] 表单文件数: {Count}, 白名单数量: {WhitelistCount}",
            form.Files.Count, _options.Value.AllowedExtensions.Count);

        foreach (var file in form.Files)
        {
            if (file.Length == 0)
            {
                _logger.LogDebug("[FileCheck] 跳过空文件: {FileName}", file.FileName);
                continue;
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext))
            {
                _logger.LogWarning("[FileCheck] 拒绝 — 无扩展名: FileName={FileName}, Size={Size}",
                    file.FileName, file.Length);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(ApiResponseResult.Failure($"无法识别文件类型: {file.FileName}", 400));
                return;
            }

            // S-26：扩展名统一小写后再匹配，防止 ".PNG"、".JPG" 等大小写变体绕过白名单
            ext = ext.ToLowerInvariant();
            var isAllowed = _options.Value.AllowedExtensions.Contains(ext);
            if (!isAllowed)
            {
                _logger.LogWarning("[FileCheck] 拒绝 — 扩展名不在白名单: Ext={Ext}, FileName={FileName}, Size={Size}",
                    ext, file.FileName, file.Length);
                context.Response.StatusCode = 400;
                // Major：不向客户端暴露白名单内容（安全加固）；统一响应 ApiResponseResult 信封
                await context.Response.WriteAsJsonAsync(ApiResponseResult.Failure($"不支持的文件类型: {ext}", 400));
                return;
            }

            _logger.LogInformation("[FileCheck] 通过: Ext={Ext}, FileName={FileName}, Size={Size}",
                ext, file.FileName, file.Length);
        }

        _logger.LogInformation("[FileCheck] 全部通过校验，放行");
        await next(context);
    }

 
    /// <summary>判断请求是否为上传文件请求</summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>是否为上传文件请求</returns>
    private static bool IsUploadRequest(HttpContext context)
    {
        var method = context.Request.Method;
        if (method != "POST" && method != "PUT" && method != "PATCH")
            return false;

        var contentType = context.Request.ContentType ?? "";
        return contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IFormCollection?> TryReadFormAsync(HttpContext context)
    {
        try
        {
            // 确保 Endpoint 路由已执行（已读取 route values 等）
            var endpointFeature = context.Features.Get<IEndpointFeature>();
            if (endpointFeature?.Endpoint is null)
                return null;

            return await context.Request.ReadFormAsync();
        }
        catch
        {
            return null;
        }
    }
}
