using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.API.APIs;

/// <summary>
/// 权限路由映射 API
/// 
/// 供网关启动时调用，获取 URL→PermissionCode 映射表。
/// 映射数据存储在 appsettings.json 的 "PermissionMappings" 节，
/// 作为权限码与路由绑定的唯一权威来源。
/// </summary>
public static class PermissionApi
{
    public static RouteGroupBuilder MapPermissionApi(this RouteGroupBuilder routeBuilder)
    {
        var route = routeBuilder.MapGroup("/permission")
            .WithHttpLogging(HttpLoggingFields.None); // 启动时高频调用，关闭日志避免噪音

        route.MapGet("/mappings", GetMappings);

        return route;
    }

    /// <summary>
    /// GET /api/ready/permission/mappings
    /// 
    /// 返回全部 URL→PermissionCode 映射，供网关启动时加载路由表。
    /// 响应格式与 NotBlog_Yarp 的 PermissionOptions.Mappings 完全兼容。
    /// </summary>
    private static IResult GetMappings([FromServices] IConfiguration configuration)
    {
        var mappings = configuration
            .GetSection("PermissionMappings")
            .Get<List<PermissionMappingDto>>();

        if (mappings is null || mappings.Count == 0)
            return Results.Ok(Array.Empty<PermissionMappingDto>());

        return Results.Ok(mappings);
    }
}

/// <summary>
/// 权限路由映射 DTO — 与 NotBlog_Yarp 侧的 RouteMapping 结构一致
/// </summary>
public sealed record PermissionMappingDto
{
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
