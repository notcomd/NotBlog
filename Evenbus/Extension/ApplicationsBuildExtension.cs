using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.Evenbus;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 初始化 EventBus（异步初始化连接和通道）
    /// 应在 app.Build() 之后调用
    /// 
    /// 用法:
    ///   var app = builder.Build();
    ///   app.UseEventBus().GetAwaiter().GetResult();
    ///   app.Run();
    /// </summary>
    public static async Task<IApplicationBuilder> UseEventBusAsync(this IApplicationBuilder app)
    {
        var eventBus = app.ApplicationServices.GetService<IEventBus>();
        if (eventBus is RabbitMqEventBus rabbitMqEventBus)
        {
            await rabbitMqEventBus.InitializeAsync();
            return app;
        }

        throw new InvalidOperationException(
            $"IEventBus 未注册或类型不正确（期望 {nameof(RabbitMqEventBus)}）。" +
            "请确保已调用 services.AddEventBus()");
    }

    /// <summary>
    /// 初始化 RequestBus（RPC 通道）
    /// </summary>
    public static async Task<IApplicationBuilder> UseRequestBusAsync(this IApplicationBuilder app)
    {
        var requestBus = app.ApplicationServices.GetService<IRequestBus>();
        if (requestBus is RabbitMqRequestBus rabbitMqRequestBus)
        {
            await rabbitMqRequestBus.InitializeAsync();
            return app;
        }

        throw new InvalidOperationException(
            $"IRequestBus 未注册或类型不正确。请确保已调用 services.AddRequestBus()");
    }
}