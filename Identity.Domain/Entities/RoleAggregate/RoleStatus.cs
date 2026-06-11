namespace Identity.Domain.Entities.RoleAggregate;

public enum RoleStatus
{
    /// <summary>
    ///     正常角色
    /// </summary>
    Normal = 0,

    /// <summary>
    ///     禁用角色
    /// </summary>
    Disabled = 1,

    /// <summary>
    ///     异常角色
    /// </summary>
    Error = 3,

    /// <summary>
    ///     删除角色
    /// </summary>
    Deleted = 2
}