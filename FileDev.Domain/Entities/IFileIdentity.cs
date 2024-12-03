namespace FileDev.Domain.Entities;

public interface IFileIdentity
{

    public bool FilePublic { get; set; }

    public bool FilePrivate { get; set; }
}