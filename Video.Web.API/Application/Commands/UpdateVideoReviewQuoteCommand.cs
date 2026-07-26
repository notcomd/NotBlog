using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>Increment/decrement a review quote (interaction counter) field.</summary>
public record UpdateVideoReviewQuoteCommand(
    Guid VideoGuid,
    Guid UserGuid,
    Guid ReviewGuid,
    string Field,
    bool IsIncrement) : IRequest<bool>;


