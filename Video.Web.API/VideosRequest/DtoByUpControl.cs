using Video.Domain.Entities;

namespace Video.Web.API.VideosRequest;

public record DtoByUpControl(Guid VideoGuid, AuthorVideo AuthorVideo, DateTimeOffset StartTime, DateTimeOffset EndTime);