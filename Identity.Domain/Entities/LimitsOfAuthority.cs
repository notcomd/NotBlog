namespace Identity.Domain.Entities;
/// <summary>
/// 权限限制
/// </summary>
public enum LimitsOfAuthority
{
    /// <summary>
    /// 根权限
    /// </summary>
    AuthorityRoot,
    
    /// <summary>
    /// 会员
    /// </summary>
    AuthorityMember,
    
    /// <summary>
    /// 用户
    /// </summary>
    AuthorityUser,
    
    /// <summary>
    /// 黑名单
    /// </summary>
    AuthorityBlack
}