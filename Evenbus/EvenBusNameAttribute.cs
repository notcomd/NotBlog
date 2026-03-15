namespace Notcomd.Evenbus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EvenBusNameAttribute(string eventBusName) : Attribute
{
    private string EventName { get; set; } = eventBusName;
}