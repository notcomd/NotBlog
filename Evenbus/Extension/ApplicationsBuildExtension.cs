﻿﻿﻿using Microsoft.Extensions.DependencyInjection;
using Notcomd.Evenbus.EventBus;

namespace Notcomd.Evenbus.Extension;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 初始化 RequestBus（RPC 通道）
    /// EventBus 的初始化由 IHostedService 自动处理，不再需要手动调用。
    /// 使用方式: await app.Services.UseRequestBusAsync();
    /// </summary>
    public static async Task UseRequestBusAsync(this IServiceProvider serviceProvider)
    {
        var requestBus = serviceProvider.GetRequiredService<IRequestBus>();
        if (requestBus is RabbitMqRequestBus rabbitMqRequestBus)
        {
            await rabbitMqRequestBus.InitializeAsync();
            return;
        }

        throw new InvalidOperationException(
            "IRequestBus 未注册或类型不正确。请确保已调用 services.AddRequestBus()");
    }
}
