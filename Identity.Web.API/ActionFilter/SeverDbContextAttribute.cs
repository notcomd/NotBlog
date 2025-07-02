namespace Identity.Web.API;

[AttributeUsage(AttributeTargets.Method)]
public class SeverDbContextAttribute : Attribute, IFilterMetadata
{
    public SeverDbContextAttribute(params Type[] dbcontexttypes)
    {
        DbContextTypes = dbcontexttypes;
    }

    public Type[] DbContextTypes { get; init; }
}