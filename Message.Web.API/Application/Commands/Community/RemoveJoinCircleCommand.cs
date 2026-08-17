namespace Message.Web.API.Application.Commands;

public record RemoveJoinCircleCommand(Guid JoinGuid,Guid UserGuid,Guid CircleGuid) : IRequest<bool>
{
    public DateTimeOffset CommandTime=DateTimeOffset.UtcNow;
}