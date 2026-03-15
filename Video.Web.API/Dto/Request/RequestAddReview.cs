using Video.Domain.ValueObjects;

namespace Video.Web.API.Dto.Request;

public record RequestAddReview(Guid UserGuid, Guid VideoGuid, string Body, Guid? RootReview,List<VideoImage>? VideoImages);