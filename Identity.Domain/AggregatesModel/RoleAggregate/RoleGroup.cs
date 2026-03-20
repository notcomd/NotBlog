namespace Identity.Domain.AggregatesModel.RoleAggregate;

public class RoleGroup : Entity, IAggregateRoot
{
    public Guid RoleGroupGuid { get; init; }

    public string RoleGroupName { get; private set; } = null!;

    public string RoleGroupCode { get; private set; }=null!;

    public HashSet<Guid>? RoleGuid { get; private set; }

    public DateTimeOffset CreatedRoleGroup { get; init; }

    public bool IsDeleted { get; private set; }

    public RoleGroup(string roleGroupName, string roleGroupCode) : this()
    {
        if (string.IsNullOrWhiteSpace(roleGroupName))
            throw new ArgumentException("Role group name cannot be null or empty", nameof(roleGroupName));
        if (string.IsNullOrWhiteSpace(roleGroupCode))
            throw new ArgumentException("Role group code cannot be null or empty", nameof(roleGroupCode));
        RoleGroupName = roleGroupName;
        RoleGroupCode = roleGroupCode;
        //RoleGuid = roleGuid;
    }

    protected RoleGroup()
    {
        RoleGroupGuid = Guid.CreateVersion7();
        RoleGuid = new HashSet<Guid>();
        CreatedRoleGroup = DateTimeOffset.UtcNow;
        IsDeleted = false;
    }

    public void ChangeRoleGroup(string roleGroupName, string roleGroupCode, HashSet<Guid> roleGuid)
    {
        RoleGroupName = roleGroupName;
        RoleGroupCode = roleGroupCode;
        RoleGuid = roleGuid;
    }

    public void RemoveRole(Guid roleGuid)
    {
        if (!RoleGuid!.Remove(roleGuid))
            throw new ArgumentException("Role not found");
    }

    public void AddRole(Guid roleGuid)
    {
        if (RoleGuid!.Add(roleGuid))
            throw new ArgumentException("Role already exists");
    }

    public void SoftDelete(bool deleted)
    {
        IsDeleted = deleted;
    }

    public void UpdateRoleGroupInfo(string roleGroupName, string roleGroupCode)
    {
        if (!string.IsNullOrWhiteSpace(roleGroupName))
            //throw new ArgumentException("Role group name cannot be null or empty", nameof(roleGroupName));
            RoleGroupName = roleGroupName;
        if (!string.IsNullOrWhiteSpace(roleGroupCode))
            RoleGroupCode = roleGroupCode;
    }
    
}