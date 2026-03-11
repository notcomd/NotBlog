namespace Notcomd.Evenbus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EvenBusNameAttribute : Attribute
{
    public EvenBusNameAttribute(string eventBusName)
    {
        EventName = eventBusName;
    }

    private string EventName { get; set; }
}