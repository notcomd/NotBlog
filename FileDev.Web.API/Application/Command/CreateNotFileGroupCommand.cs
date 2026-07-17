namespace FileDev.Web.API.Application.Command;

public class CreateNotFileGroupCommand : IRequest<bool>
{
    public Guid UserGuid { get; set; }
    public string FileGroupName { get; set; } = null!;
    public HashSet<string>? FileGroupTags { get; set; }
    public string? FileGroupDescription { get; set; }

    public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePrivate;
}
