namespace Notcomd.Token.JWT.Core;

/// <summary>
/// JWT 权限相关自定义 Claim 类型名（Identity 签发侧与 YARP 网关消费侧共享同一程序集，常量不会漂移）
/// </summary>
public static class PermissionClaimTypes
{
    /// <summary>
    /// 用户全部有效权限码（角色直连 + 角色组继承，去重，逗号分隔）。
    /// 授权目录码 = 自动放行其全部子孙（网关按前缀段匹配，与 Identity PermissionChecker 同逻辑）。
    /// </summary>
    public const string Permissions = "permissions";

    /// <summary>
    /// 数据范围，格式 "type|value1,value2,..."（与 DataScope.ToClaimValue 一致），如 "2|" 表示全部数据。
    /// </summary>
    public const string DataScope = "data_scope";
}
