using System.Text.Json.Serialization;

namespace Notcomd.Evenbus;

/// <summary>
/// 集成事件基类
/// 所有集成事件（跨服务通信）应继承此类
/// </summary>
public record IntegrationEvent
{
    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonInclude]
    public Guid Id { get; set; }

    [JsonInclude]
    public DateTime CreationDate { get; set; }
}
