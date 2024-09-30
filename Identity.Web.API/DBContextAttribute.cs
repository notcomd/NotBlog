namespace Identity.Web.API;

[AttributeUsage(AttributeTargets.Method)]
public class DBContextAttribute : Attribute
{
    public DBContextAttribute(params Type[] dbcontexttypes)
    {
        DbContextTypes = dbcontexttypes;
    }

    public Type[] DbContextTypes { get; init; }
}