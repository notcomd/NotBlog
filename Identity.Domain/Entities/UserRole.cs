namespace Identity.Domain.Entities;

public class UserRole : IAggregateRoot
{
    /// <summary>
    /// 角色权限
    /// </summary>
    private LimitsOfAuthority LimitsOfAuthority;

    private UserRole()
    {
    }

    public UserRole(string roleName)
    {
        UserRoleGuid = new Guid();
        RoleName = roleName;
        this.LimitsOfAuthority = Entities.LimitsOfAuthority.AuthorityUser;
        if (LimitsOfAuthority == LimitsOfAuthority.AuthorityUser)
        {
            RoleEndTime = DateTime.Now.AddYears(999);
        }
        else if (LimitsOfAuthority == LimitsOfAuthority.AuthorityMember)
        {
            RoleEndTime = DateTime.Now.AddYears(1);
        }
        else if (LimitsOfAuthority == LimitsOfAuthority.AuthorityRoot)
        {
            RoleEndTime = DateTime.Now.AddYears(999);
        }
    }

    public Guid UserRoleGuid { get; init; }

    /// <summary>
    /// 角色名
    /// </summary>
    public string RoleName { get; private set; } = null!;

    /// <summary>
    /// 角色过期时间
    /// </summary>
    [Column(TypeName = "timestamp with time zone")]
    public DateTime? RoleEndTime { get; private set; }


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

    private void ChangeByLimitOfAuthorize(LimitsOfAuthority limitsOfAuthority)
    {
        this.LimitsOfAuthority = limitsOfAuthority;
    }
}