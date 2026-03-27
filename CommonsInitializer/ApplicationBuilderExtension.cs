using Microsoft.AspNetCore.Builder;

namespace CommonsInitializer;

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
        // 基础中间件由各宿主项目自行配置：
        // app.UseCors();
        // app.UseForwardedHeaders();
        // app.UseAuthentication();
        // app.UseAuthorization();
        return app;
    }
}