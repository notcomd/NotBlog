var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ═══ YARP 反向代理（透明转发）═══
// 2026-09-06 权限验证已下沉各服务：网关不再做 JWT 认证/权限判定/Header 注入，
// 目标服务基于 JWT 本地完成认证与权限校验（RequirePermission/RequireResourcePermissions 标注 +
// PermissionEnforcementMiddleware）。设计文档：docs/权限验证下沉设计-网关回收与服务自验.md
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseWebSockets();

app.MapReverseProxy();

app.Run();
