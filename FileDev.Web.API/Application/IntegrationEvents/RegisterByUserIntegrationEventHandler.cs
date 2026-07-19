using Evenbus.Core;
using FileDev.Domain.IRepository;
using Microsoft.Extensions.Logging;
using Notcomd.Evenbus;
using FileDev.Web.API.Application.Command;

namespace FileDev.Web.API.Application.IntegrationEvents;

/// <summary>
/// 消费 Identity 服务发布的用户注册事件。
/// 路由键: RegisterByUserIntegrationEvent
/// </summary>
public class RegisterByUserIntegrationEventHandler(ILogger<RegisterByUserIntegrationEventHandler> logger,
                                                   //INotFileGroupRepository notFileGroupRepository,
                                                   INotMediator notMediator)
    : IIntegrationEventHandler<RegisterByUserIntegrationEvent>
{
    public async Task Handler(RegisterByUserIntegrationEvent @event)
    {
        if (@event is null)
            return;
        logger.LogInformation(
            "[Integration] 收到用户注册事件: UserId={UserId}, RegisterTime={RegisterTime}",
            @event.UserId, @event.RegisterTime);

        var commandCreateFileGroup = new CreateNotFileGroupCommand
        {
            UserGuid = @event.UserId,
            FileGroupName = "默认文件组",
            FileIdentity = FileIdentity.FilePublic,
            ParentGroupId = Guid.Empty,
            FileGroupTags = [],
            FileGroupDescription = "默认文件组"
        };
        await notMediator.SendAsync(commandCreateFileGroup);
        logger.LogInformation(
            "[Integration] 创建默认文件组成功: UserId={UserId}, RegisterTime={RegisterTime}",
            @event.UserId, @event.RegisterTime);
    }
}

/// <summary>
/// 用户注册集成事件 — 与 Identity 服务发布的 JSON 结构一致。
/// 放在消费端项目中避免对 Identity 项目的编译依赖。
/// </summary>
public record RegisterByUserIntegrationEvent(Guid UserId) : IntegrationEvent
{
    public DateTime RegisterTime { get; init; }
}
