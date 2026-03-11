using Microsoft.AspNetCore.Builder;
using Notcomd.Evenbus;

namespace CommonsInitializer;

public static class ApplicationBuilderExtension
{
    public static IApplicationBuilder NotBlogUseServer(this IApplicationBuilder app)
    {
        app.UseEventBus();
        app.UseCors(); //启用Cors
        app.UseForwardedHeaders();
        //app.UseHttpsRedirection();//不能与ForwardedHeaders很好的工作，而且webapi项目也没必要配置这个
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}