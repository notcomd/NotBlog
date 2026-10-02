namespace Identity.Domain.Entities.MenuAggregate;

/// <summary>
/// 菜单类型
/// </summary>
public enum MenuType
{
    /// <summary>
    ///     目录（模块分组，可包含子菜单，通常无路由）
    /// </summary>
    Directory = 1,

    /// <summary>
    ///     菜单项（可点击跳转的页面入口）
    /// </summary>
    Item = 2,
}