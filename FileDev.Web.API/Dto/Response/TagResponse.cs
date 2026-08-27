
namespace FileDev.Web.API.Dto;

public sealed record TagResponse(
    Guid TagId,
    string TagName,
    string? TagDescription,
    bool IsDefault,
    IReadOnlyList<Guid> FileIds,
    DateTimeOffset UploadTime,
    DateTimeOffset UpdateTime);