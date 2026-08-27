using FileDev.Domain;
using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Domain.IRepository;
using Notcomd.EventBus.Core;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 消费 Identity 服务发布的用户注册事件，为新用户预置 4 个默认标签（图片/文档/文件/视频）。
/// 路由键: RegisterByUserIntegrationEvent。
/// 进入点幂等：以"用户+标签名"唯一约束兜底，重复消费时已存在的默认标签不重复插入。
/// </summary>
public class RegisterByUserIntegrationEventHandler(
    ILogger<RegisterByUserIntegrationEventHandler> logger,
    INotFileTagRepository notFileTagRepository)
    : IIntegrationEventHandler<RegisterByUserIntegrationEvent>
{
    public async Task Handler(RegisterByUserIntegrationEvent @event)
    {
        if (@event is null)
            return;
        logger.LogInformation(
            "[Integration] 收到用户注册事件: UserId={UserId}, RegisterTime={RegisterTime}",
            @event.UserId, @event.RegisterTime);

        foreach (var kind in FileTagDefaults.DefaultKinds)
        {
            // 已存在同名标签（含用户此前自定义的同名标签）则跳过，避免唯一约束冲突
            var name = FileTagDefaults.GetDefaultName(kind);
            if (await notFileTagRepository.GetNotFileTagByNameAsync(@event.UserId, name) != null)
                continue;

            var tag = new NotFileTag(@event.UserId, name, $"系统预置的默认标签（{name}）", kind);
            await notFileTagRepository.InsertNotFileTagAsync(tag);
        }

        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync();
        logger.LogInformation(
            "[Integration] 预置默认标签完成: UserId={UserId}, Count={Count}",
            @event.UserId, FileTagDefaults.DefaultKinds.Count);
    }
}