using FileDev.Domain.IServices;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的作用域租户上下文访问器，实现 <see cref="ITenantContext"/>。
/// <para>
/// 使用静态 AsyncLocal 承载当前调用链的租户 ID：HTTP 中间件从 JWT 用户上下文解析后写入、
/// gRPC 拦截器从 <c>ServerCallContext.UserState</c> 解析后写入，随异步调用链自动传播，
/// 供存储层读取以派生 FileBox 命名空间。
/// </para>
/// </summary>
public sealed class AsyncLocalTenantContext : ITenantContext
{
    private static readonly AsyncLocal<string?> Current = new();

    /// <summary>静态入口：供 HTTP 中间件 / gRPC 拦截器写入当前租户 ID。</summary>
    public static void SetTenantId(string? tenantId)
        => Current.Value = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;

    /// <inheritdoc/>
    public string TenantId => string.IsNullOrWhiteSpace(Current.Value)
        ? ITenantContext.DefaultTenantId
        : Current.Value;
}