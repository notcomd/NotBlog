using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>Increment a video quote (interaction counter) field.</summary>
public record UpdateVideoQuoteCommand(Guid VideoGuid, Guid UserGuid, string Field, bool IsAdd) : IRequest<bool>;

