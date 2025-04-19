using DomainCommonst;

namespace Identity.Domain.Entities;

public class Roles : IAggregateRoot
{

    public Guid UserGuid { get; private set; }
    public Guid UserRoleGuid { get; private set; }
    public User User { get; private set; }

    public UserRole UserRole { get; private set; }

    public string? Attribute { get; private set; }


    public Task<Roles> AddByRoleAsync(Guid userRole, Guid roleGuid)
    {
        UserGuid = userRole;
        UserRoleGuid = roleGuid;
        return Task.FromResult(this);
    }

    public Task<Roles> ChangeByRolesAsync(Guid userRole)
    {
        if (UserGuid == userRole)
        {
            throw new AggregateException("");
        }
        UserGuid = userRole;
        return Task.FromResult(this);
    }
}