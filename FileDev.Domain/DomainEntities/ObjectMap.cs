namespace FileDev.Domain.DomainEntities;

public record ObjectMap
{
    public ObjectMap(string objectKey, string physicalName)
    {
        ObjectKey = objectKey;
        PhysicalName = physicalName;
    }

    public string ObjectKey { get; set; } = null!;

    public string PhysicalName { get; set; } = null!;

}
