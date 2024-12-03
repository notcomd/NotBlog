namespace Identity.Domain.Entities;

public class Roles : IAggregateRoot
{

    private Roles() {}

    public Roles(Guid roleGuid, Guid userGuid)
    {
        UserGuid = userGuid;
        RoleGuid = roleGuid;
    }
    public Guid UserGuid { get; init; }

    public Guid RoleGuid { get; init; }
}