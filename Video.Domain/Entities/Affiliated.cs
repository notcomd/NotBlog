namespace Video.Domain.Entities;

/// <summary>
///     用于用户权限
/// </summary>
/// <param name="AffiliatedUserUuid">用户唯一标识</param>
/// <param name="AffiliatedAuthorize">所属权限</param>
public record Affiliated(Guid AffiliatedUserUuid, AffiliatedAuthorize AffiliatedAuthorize);