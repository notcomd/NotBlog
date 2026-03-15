namespace Notcomd.Evenbus;

public class IntegrationEventRabbitMqOptions
{
    public string HostName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string? UserName { get; set; }
    public string? Password { get; set; }
}