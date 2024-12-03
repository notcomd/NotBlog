using Identity.Domain.Entities;

namespace Identity.Web.API.ActionFilter;

[AttributeUsage(AttributeTargets.Method)]
public class UserLimitsOfAuthorityAttribute(LimitsOfAuthority limitsOfAuthority) : Attribute
{
    public LimitsOfAuthority LimitsOfAuthority { get; set; } = limitsOfAuthority;
}