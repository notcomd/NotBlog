using Microsoft.AspNetCore.Builder;

namespace Commons.Web;

/// <summary>
/// IApplicationBuilder 扩展方法
/// 中间件管道配置的扩展点
/// 具体实现在各宿主项目中完成
/// </summary>
public static class ApplicationBuilderExtension
{
    /// <summary>
    /// NotBlog 项目中间件管道配置入口
    /// 宿主项目可在此前后添加自定义中间件
    /// </summary>
    public static IApplicationBuilder UseNotBlogPipeline(this IApplicationBuilder app)
    {
        // 全局异常脱敏（S-16）：必须位于管道最前，捕获后续所有中间件/端点的未处理异常
        app.UseMiddleware<ExceptionSanitizingMiddleware>();

        // 统一响应包装（ApiResponseResult 信封）：位于脱敏中间件之后，
        // 使其只包装正常返回的端点响应，未处理异常已由脱敏中间件直接输出统一信封
        app.UseApiResponseWrapping();

        // CORS（S-15）：仅当宿主已注册默认 CORS 策略时启用。
        // 未调用 AddCors 的宿主（如 FileDev）若直接 UseCors() 会在管线构建时抛
        // "Unable to find the required services" 异常，因此先探测服务是否注册。
        var corsServiceType = Type.GetType(
            "Microsoft.AspNetCore.Cors.Infrastructure.ICorsService, Microsoft.AspNetCore.Cors");
        if (corsServiceType is not null &&
            app.ApplicationServices.GetService(corsServiceType) is not null)
        {
            app.UseCors();
        }

        // 认证 / 授权（S-03）：RequireAuthorization 端点生效的前提
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// 挂接全局异常脱敏中间件（S-16），供未调用 <see cref="UseNotBlogPipeline"/> 的宿主单独使用。
    /// 必须在管道最前调用。
    /// </summary>
    public static IApplicationBuilder UseNotBlogExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionSanitizingMiddleware>();
    }

    /// <summary>
    /// 挂接统一响应包装中间件（ApiResponseResult 信封）。
    /// <para>
    /// 供未调用 <see cref="UseNotBlogPipeline"/> 的宿主（Message/Markdown/Video 等）单独调用。
    /// 需注册在异常处理中间件之后、认证中间件之前；<see cref="UseNotBlogPipeline"/> 已包含本中间件，无需重复调用。
    /// </para>
    /// </summary>
    public static IApplicationBuilder UseApiResponseWrapping(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiResponseWrappingMiddleware>();
    }
}
