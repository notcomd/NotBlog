using NotMediator;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

public record DeleteVideoCommand(Guid VideoGuid,Guid UserGuid) : IRequest<bool>;

