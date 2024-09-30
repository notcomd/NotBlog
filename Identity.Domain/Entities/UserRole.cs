namespace Identity.Domain.Entities;

public class UserRole : IAggregateRoot
{
    private UserRole()
    {
    }

    public UserRole(string roleName)
    {
        UserRoleGuid = new Guid();
        RoleName = roleName;
        this.LimitsOfAuthority = Entities.LimitsOfAuthority.AuthorityUser;
    }

    public Guid UserRoleGuid { get; init; }

    private LimitsOfAuthority LimitsOfAuthority;
    public string RoleName { get; private set; } = null!;

    /// <summary>
    ///     应用该角色人数
    /// </summary>
    public long Roles { get; private set; }

    /// <summary>
    ///     角色过期时间
    /// </summary>
    public DateTime? RoleEndTime { get; private set; }

    private void AddByRolesAsync()
    {
        Roles++;
    }

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

    private void ChangeByLimitOfAuthorize(LimitsOfAuthority limitsOfAuthority)
    {
        this.LimitsOfAuthority = limitsOfAuthority;
    }
}