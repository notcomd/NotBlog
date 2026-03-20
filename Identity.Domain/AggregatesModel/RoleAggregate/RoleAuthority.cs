namespace Identity.Domain.AggregatesModel.RoleAggregate;

public enum RoleAuthority
{
    /// <summary>
    ///     根
    /// </summary>
    Root = 0,

    /// <summary>
    ///     管理员
    /// </summary>
    Admin = 1,

    /// <summary>
    ///     用户
    /// </summary>
    User = 2,

    /// <summary>
    ///     游客
    /// </summary>
    Guest = 3,

    /// <summary>
    ///  无权限
    /// </summary>
    Unknown = 4
}