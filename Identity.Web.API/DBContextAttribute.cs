namespace Identity.Web.API;

[AttributeUsage(AttributeTargets.Method)]
public class DBContextAttribute:Attribute
{
    public Type[]  DbContextTypes { get; init; }

    public DBContextAttribute(params Type[] dbcontexttypes) => (DbContextTypes) = (dbcontexttypes);
}