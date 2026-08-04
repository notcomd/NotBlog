using Notcomd.EventBus.Core;
using FileDev.Web.API.Application.Command;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 消费 Identity 服务发布的用户注册事件。
/// 路由键: RegisterByUserIntegrationEvent
/// </summary>
public class RegisterByUserIntegrationEventHandler(ILogger<RegisterByUserIntegrationEventHandler> logger,
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
            FileGroupTags = [],
            FileGroupDescription = "默认文件组"
        };
        await notMediator.SendAsync(commandCreateFileGroup);
        logger.LogInformation(
            "[Integration] 创建默认文件组成功: UserId={UserId}, RegisterTime={RegisterTime}",
            @event.UserId, @event.RegisterTime);
    }
}
