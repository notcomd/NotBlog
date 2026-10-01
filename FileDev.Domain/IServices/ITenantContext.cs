namespace FileDev.Domain.IServices;

/// <summary>
/// 租户上下文访问器：暴露当前请求所属租户 ID。
/// <para>
/// 租户 ID 取自 JWT 中 identity/nameidentifier（即当前用户 ID），用于存储层派生 FileBox 命名空间
/// 做多租户逻辑隔离；未解析到用户上下文时回退默认租户。
/// </para>
/// </summary>
public interface ITenantContext
{
    /// <summary>当前租户 ID；无用户上下文时返回 <see cref="DefaultTenantId"/>。</summary>
    string TenantId { get; }

    /// <summary>默认租户 ID。</summary>
    static string DefaultTenantId => "notblog";
}