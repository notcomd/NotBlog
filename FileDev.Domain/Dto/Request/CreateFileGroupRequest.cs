
namespace FileDev.Domain.Dto.Request;

public class CreateFileGroupRequest
{
    public string Name { get; set; } = string.Empty;

    public HashSet<string> GroupTags { get; set; } = new();

    public string? Description { get; set; }


}
