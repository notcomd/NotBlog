using System.Reflection;
using System.Text;
using System.Text.Json;
using Evenbus.IntegrationTest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notcomd.Evenbus;
using Notcomd.Evenbus.EventBus;
using Notcomd.Evenbus.Extension;
using RabbitMQ.Client;

// ════════════════════════════════════════════════════════════════
//  Evenbus 集成测试 — 真实 RabbitMQ 连接验证
// ════════════════════════════════════════════════════════════════

try
{
    Console.Title = "Evenbus 集成测试";
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("""
        ╔══════════════════════════════════════════════╗
        ║   Evenbus 集成测试 — RabbitMQ 真实连接验证   ║
        ╚══════════════════════════════════════════════╝
        """);
    Console.ResetColor();

    // ── 读取配置 ──────────────────────────────────────────────
    var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    var userName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
    var password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
    var exchangeName = "notcomd_test_exchange";
    var queueName = "test_order_queue";

    Console.WriteLine($"  RabbitMQ 地址: {hostName}:5672");
    Console.WriteLine($"  用户名:        {userName}");
    Console.WriteLine($"  Exchange:      {exchangeName}");
    Console.WriteLine($"  队列:          {queueName}");
    Console.WriteLine();

    // ── 构建 DI 容器 ─────────────────────────────────────────
    var services = new ServiceCollection();

    // 注册真实的 IConnectionFactory
    services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
    {
        HostName = hostName,
        UserName = userName,
        Password = password,
        Port = 5672
    });

    // 配置 EventBus
    services.Configure<IntegrationEventRabbitMqOptions>(o =>
    {
        o.ExchangeName = exchangeName;
    });

    services.Configure<EventBusOptions>(o =>
    {
        o.SubscriptionClientName = queueName;
        o.RetryCount = 3;
    });

    // 注册 EventBus（自动扫描当前 Assembly 中的 Handler）
    services.AddEventBus(queueName, Assembly.GetExecutingAssembly());

    // 添加日志
    services.AddLogging();

    var sp = services.BuildServiceProvider();
    var eventBus = sp.GetRequiredService<IEventBus>();

    // ── 检查实际配置的 Exchange 名称 ──────────────────────────
    var rabbitOpts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationEventRabbitMqOptions>>().Value;
    Console.WriteLine($"  [Debug] RabbitMqEventBus 实际使用的 Exchange: {rabbitOpts.ExchangeName}");
    Console.WriteLine();

    // ── 构造测试事件 ────────────────────────────────────────
    var testEvent = new OrderConfirmedIntegrationEvent(
        OrderId: Guid.NewGuid(),
        CustomerName: "张三",
        Amount: 1288.88m,
        ProductName: "NotBlog Pro 年度订阅",
        ConfirmedAt: DateTime.UtcNow
    );

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  即将发布集成事件:");
    Console.WriteLine($"    事件类型: {testEvent.GetType().Name}");
    Console.WriteLine($"    事件ID:   {testEvent.Id}");
    Console.WriteLine($"    路由键:   {testEvent.GetType().Name}");
    Console.WriteLine($"    订单ID:   {testEvent.OrderId}");
    Console.WriteLine($"    客户:     {testEvent.CustomerName}");
    Console.WriteLine($"    金额:     {testEvent.Amount:C}");
    Console.WriteLine($"    商品:     {testEvent.ProductName}");
    Console.ResetColor();
    Console.WriteLine();

    // ── 发布事件 ────────────────────────────────────────────
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("  >>> 正在发布到 RabbitMQ ...");
    Console.ResetColor();

    // ── 发布前：预先声明并绑定队列到 Exchange ────────────────
    var factory = sp.GetRequiredService<IConnectionFactory>();
    await using var prepConnection = await factory.CreateConnectionAsync();
    await using var prepChannel = await prepConnection.CreateChannelAsync();

    await prepChannel.ExchangeDeclareAsync(exchangeName, "direct", durable: true);

    // 清理旧队列（避免旧消息干扰）
    try { await prepChannel.QueueDeleteAsync(queueName); } catch { /* 忽略 */ }

    await prepChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
    await prepChannel.QueueBindAsync(queueName, exchangeName, testEvent.GetType().Name);

    // 确认队列初始为空
    var initialCount = (await prepChannel.QueueDeclarePassiveAsync(queueName)).MessageCount;
    Console.WriteLine($"  队列初始消息数: {initialCount}");

    // 确认 binding 成功: 手动发一条测试消息
    var testMsgId = Guid.NewGuid().ToString();
    var testBody = Encoding.UTF8.GetBytes("test-probe");
    var testProps = new BasicProperties { MessageId = testMsgId };
    await prepChannel.BasicPublishAsync(
        exchange: exchangeName,
        routingKey: testEvent.GetType().Name,
        mandatory: false,
        basicProperties: testProps,
        body: testBody);

    await Task.Delay(200);
    var afterProbe = (await prepChannel.QueueDeclarePassiveAsync(queueName)).MessageCount;
    Console.WriteLine($"  Binding 验证（手动发 probe 消息后）: {afterProbe} 条");

    // 清空队列
    if (afterProbe > 0)
        await prepChannel.QueuePurgeAsync(queueName);

    Console.WriteLine();

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await eventBus.PublishAsync(testEvent);
    sw.Stop();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  >>> 发布成功！耗时: {sw.ElapsedMilliseconds}ms");
    Console.ResetColor();
    Console.WriteLine();

    // ── 等待一下，让消费者有机会处理 ────────────────────────
    Console.WriteLine("  等待 3 秒，让消费者处理消息...");
    await Task.Delay(3000);

    // ── 验证消息是否到达队列 ────────────────────────────────
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ── 验证 RabbitMQ 队列中的消息 ──");
    Console.ResetColor();

    var factory2 = sp.GetRequiredService<IConnectionFactory>();
    await using var verifyConnection = await factory2.CreateConnectionAsync();
    await using var verifyChannel = await verifyConnection.CreateChannelAsync();

    var queueInfo = await verifyChannel.QueueDeclarePassiveAsync(queueName);
    Console.WriteLine($"  队列消息数: {queueInfo.MessageCount}");
    Console.WriteLine($"  消费者数:   {queueInfo.ConsumerCount}");

    if (queueInfo.MessageCount > 0)
    {
        var result = await verifyChannel.BasicGetAsync(queueName, autoAck: true);
        if (result != null)
        {
            var body = Encoding.UTF8.GetString(result.Body.Span);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"""
                已从 RabbitMQ 队列中读取到消息:
                  消息ID:      {result.BasicProperties?.MessageId}
                  DeliveryTag: {result.DeliveryTag}
                  RoutingKey:  {result.RoutingKey}
                  Exchange:    {result.Exchange}
                  消息体:
                {JsonSerializer.Serialize(
                    JsonSerializer.Deserialize<object>(body),
                    new JsonSerializerOptions { WriteIndented = true })}
                """);
            Console.ResetColor();

            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var restored = JsonSerializer.Deserialize<OrderConfirmedIntegrationEvent>(body, jsonOptions)!;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  数据一致性: {restored.OrderId == testEvent.OrderId
                && restored.CustomerName == testEvent.CustomerName
                && restored.Amount == testEvent.Amount}");
            Console.ResetColor();
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("  警告: 队列中无消息！");
        Console.ResetColor();
    }

    Console.WriteLine();

    // ── 200 并发发布验证 ──────────────────────────────────
    await prepChannel.QueuePurgeAsync(queueName);
    var initialCount2 = (await prepChannel.QueueDeclarePassiveAsync(queueName)).MessageCount;
    Console.WriteLine($"  队列已清空，当前消息数: {initialCount2}");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("  ── 200 并发发布验证 ──");
    Console.ResetColor();

    const int concurrentCount = 20000;
    var tasks = new Task[concurrentCount];
    var concurrentSw = System.Diagnostics.Stopwatch.StartNew();
    int successCount = 0;
    int failCount = 0;

    for (int i = 0; i < concurrentCount; i++)
    {
        var idx = i;
        tasks[i] = Task.Run(async () =>
        {
            try
            {
                var evt = new OrderConfirmedIntegrationEvent(
                    OrderId: Guid.NewGuid(),
                    CustomerName: $"并发用户-{idx:D3}",
                    Amount: 100m + idx,
                    ProductName: $"并发商品-{idx:D3}",
                    ConfirmedAt: DateTime.UtcNow
                );
                await eventBus.PublishAsync(evt);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failCount);
                Console.WriteLine($"  [失败 #{idx}] {ex.GetType().Name}: {ex.Message}");
            }
        });
    }

    await Task.WhenAll(tasks);
    concurrentSw.Stop();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"""
         并发发布完成:
           总数:   {concurrentCount}
           成功:   {successCount}
           失败:   {failCount}
           总耗时: {concurrentSw.ElapsedMilliseconds}ms
        """);
    Console.ResetColor();

    // 等待消息全部路由到队列
    await Task.Delay(2000);

    // 验证队列中的消息数
    var concurrentQueueInfo = await prepChannel.QueueDeclarePassiveAsync(queueName);
    Console.ForegroundColor = concurrentQueueInfo.MessageCount == concurrentCount
        ? ConsoleColor.Green
        : ConsoleColor.Red;
    Console.WriteLine($"  队列消息数: {concurrentQueueInfo.MessageCount} / {concurrentCount}");
    Console.ResetColor();

    // 抽样校验最后 5 条
    Console.WriteLine("  抽样校验最后 5 条消息:");
    for (int i = 0; i < 5; i++)
    {
        var getResult = await verifyChannel.BasicGetAsync(queueName, autoAck: true);
        if (getResult != null)
        {
            var body = Encoding.UTF8.GetString(getResult.Body.Span);
            var restored = JsonSerializer.Deserialize<OrderConfirmedIntegrationEvent>(
                body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Console.WriteLine($"    [{i + 1}] OrderId={restored.OrderId.ToString()[..8]}...  " +
                              $"Customer={restored.CustomerName}  Amount={restored.Amount}");
        }
    }

    // 清空剩余消息
    await prepChannel.QueuePurgeAsync(queueName);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ═══════════════════════════════════════");
    Console.WriteLine("   集成测试完成！");
    Console.WriteLine("  ═══════════════════════════════════════");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"""

        ╔══════════════════════════════════════════════╗
        ║        测试失败！                              ║
        ╠══════════════════════════════════════════════╣
        ║  错误: {ex.GetType().Name,-37} ║
        ║  消息: {Truncate(ex.Message, 36),-37} ║
        ╚══════════════════════════════════════════════╝

        完整堆栈:
        {ex}
        """);
    Console.ResetColor();

    if (ex is RabbitMQ.Client.Exceptions.BrokerUnreachableException)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("""

            提示: 请确保 RabbitMQ 正在运行。
            可以通过 docker 启动:
              docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
            或者设置环境变量:
              RABBITMQ_HOST=your-host
              RABBITMQ_USER=your-user
              RABBITMQ_PASS=your-pass
            """);
        Console.ResetColor();
    }

    Environment.ExitCode = 1;
}
finally
{
    Console.ResetColor();
}

static string Truncate(string value, int maxLength) =>
    value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
