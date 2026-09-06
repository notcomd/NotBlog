namespace Identity.Domain.Entities.RoleAggregate;

/// <summary>
/// 权限类型
/// </summary>
public enum PermissionType
{
    /// <summary>
    ///     目录/菜单节点（模块分组，可包含子节点；授权目录 = 自动拥有全部子孙权限）
    /// </summary>
    Menu = 1,

    /// <summary>
    ///     按钮权限（页面内操作点，预留）
    /// </summary>
    Button = 2,

    /// <summary>
    ///     API 接口权限（叶子；网关按 PermissionCode 匹配）
    /// </summary>
    Api = 3,
}
