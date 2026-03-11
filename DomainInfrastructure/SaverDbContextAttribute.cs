namespace Notcomd.DomainCommand;

public class SaverDbContextAttribute : Attribute
{
    public SaverDbContextAttribute(params Type[] dbcontexttypes)
    {
        DbContextTypes = dbcontexttypes;
    }

    public Type[] DbContextTypes { get; set; }
}