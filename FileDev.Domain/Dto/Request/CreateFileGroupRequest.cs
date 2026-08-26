
namespace FileDev.Domain.Dto.Request
{
    public class CreateFileGroupRequest
    {
        public string Name { get; set; } = string.Empty;

        public HashSet<string> GroupTags { get; set; } = [];

        public string? Description { get; set; } = null;

        public Guid? ParentGroupId { get; set; }
    
        public FileIdentity FileIdentity { get; set; } = FileIdentity.FilePublic;

    }


}
