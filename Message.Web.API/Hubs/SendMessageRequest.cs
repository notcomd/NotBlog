using Message.Domain.Enums;

namespace Message.Web.API.Hubs;

public abstract record SendMessageRequest
{
    public MessageType MessageType { get; init; }
    public string? Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
    public string? MimeType { get; init; }
    public double? Duration { get; init; }
    public string? Caption { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationName { get; init; }
    public string? LinkUrl { get; init; }
    public string? LinkTitle { get; init; }
    public string? LinkDescription { get; init; }
    public string? ExpressionCode { get; init; }
}