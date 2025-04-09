namespace Notcomd.Evenbus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IEvenBusAttribute : Attribute
{
    public IEvenBusAttribute(string eventBusName)
    {
        EventName = eventBusName;
    }
    private string EventName { get; set; }
}