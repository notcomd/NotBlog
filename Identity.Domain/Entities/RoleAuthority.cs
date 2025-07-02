namespace Identity.Domain.Entities;

public enum RoleAuthority
{
    /// <summary>
    /// 根
    /// </summary>
    Root,

    /// <summary>
    /// 管理员
    /// </summary>
    Admin,

    /// <summary>
    /// 成员
    /// </summary>
    Member,

    /// <summary>
    /// 用户
    /// </summary>
    User,

    /// <summary>
    /// 游客
    /// </summary>
    Guest,

    /// <summary>
    /// 默认
    /// </summary>
    None,

    /// <summary>
    /// 未知
    /// </summary>
    Unknown,

    /// <summary>
    /// 默认
    /// </summary>
    Default,

    /// <summary>
    /// 其他
    /// </summary>
    Other,

    /// <summary>
    /// 未知角色
    /// </summary>
    UnknownRole,

    /// <summary>
    /// 未知用户
    /// </summary>
    UnknownUser,

    /// <summary>
    /// 未知权限
    /// </summary>
    UnknownRoleAuthority,
}