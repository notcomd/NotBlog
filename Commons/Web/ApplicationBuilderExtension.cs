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
}
