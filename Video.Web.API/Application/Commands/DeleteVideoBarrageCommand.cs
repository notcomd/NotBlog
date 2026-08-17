namespace Video.Web.API.Application.Commands;


public record DeleteVideoBarrageCommand(Guid VideoGuid,Guid VideoBarrageGuid) : IRequest<bool>;
