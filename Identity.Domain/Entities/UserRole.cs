namespace Identity.Domain.Entities;

public class UserRole : IAggregateRoot
{
    private UserRole()
    {

    }

    public UserRole(string roleName)
    {
        UserRoleGuid = Guid.NewGuid();
        RoleName = roleName;
        LimitsOfAuthority = LimitsOfAuthority.AuthorityUser;
        Roles = new Roles();
    }


    public Guid UserRoleGuid { get; init; }
    /// <summary>
    ///     角色名
    /// </summary>
    public string RoleName { get; private set; } = null!;
    /// <summary>
    ///     角色过期时间
    /// </summary>
    public DateTimeOffset? RoleEndTime { get; private set; }

    /// <summary>
    ///     角色权限
    /// </summary>
    public LimitsOfAuthority LimitsOfAuthority { get; set; }

    public Roles Roles { get; private set; }


    public ValueTask<bool> IsRoleAsync(string roleName)
    {
        return new ValueTask<bool>(RoleName == roleName);
    }

    public ValueTask SetRoleEndTimeValueTask(DateTime newEndTime)
    {
        RoleEndTime = newEndTime;
        return ValueTask.CompletedTask;
    }

    public bool IsRoleExpired()
    {
        if (RoleEndTime < DateTime.Now)
        {
            RoleName = string.Empty;
            return true;
        }

        return false;
    }

    public void ResetLimitOfAuthorize()
    {
        if (RoleEndTime < DateTime.Now && LimitsOfAuthority == LimitsOfAuthority.AuthorityMember)
        {
            ChangeByLimitOfAuthorize(LimitsOfAuthority.AuthorityUser);
        }
    }

    private LimitsOfAuthority ChangeByLimitOfAuthorize(LimitsOfAuthority limitsOfAuthority)
    {
        return LimitsOfAuthority = limitsOfAuthority;
    }
}