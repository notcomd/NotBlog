namespace DomainCommons;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    void SoftDelete();
}