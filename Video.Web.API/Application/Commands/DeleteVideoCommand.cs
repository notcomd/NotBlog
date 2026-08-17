using NotMediator;

namespace Video.Web.API.Application.Commands;

public record DeleteVideoCommand(Guid VideoGuid,Guid UserGuid) : IRequest<bool>;

