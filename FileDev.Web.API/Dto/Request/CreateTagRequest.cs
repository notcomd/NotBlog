
namespace FileDev.Web.API.Dto.Request;

public sealed record CreateTagRequest(string Name, string? Description = null);