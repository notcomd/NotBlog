namespace Notcomd.Evenbus;

public class IntegrationEventRabbitMQOptions
{
    public string HostName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string? UserName { get; set; }
    public string? Password { get; set; }
}