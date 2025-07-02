namespace Identity.Domain.Entities;

/// <summary>
///     权限限制
/// </summary>
public enum LimitsOfAuthority
{
    /// <summary>
    ///     根权限
    /// </summary>
    AuthorityRoot,

    /// <summary>
    ///     管理员
    /// </summary>
    AuthorityAdmin,

    /// <summary>
    ///     会员
    /// </summary>
    AuthorityMember,

    /// <summary>
    ///     网格
    /// </summary>
    AuthorityGrid,

    /// <summary>
    ///     用户
    /// </summary>
    AuthorityUser,

    /// <summary>
    ///  黑名单
    /// </summary>
    AuthorityBlack,

    /// <summary>
    ///     白名单
    /// </summary>
    AuthorityWhite,

    /// <summary>
    ///     无权限
    /// </summary>
    AuthorityNone,

    /// <summary>
    ///     未知权限
    /// </summary>
    AuthorityUnknown,

    /// <summary>
    ///     游客
    /// </summary>
    AuthorityGuest
}