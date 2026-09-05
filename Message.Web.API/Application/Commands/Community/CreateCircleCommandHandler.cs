
namespace Message.Web.API.Application.Commands.Community;
/// <summary>
/// 创建圈子命令处理程序。
/// <para>联动：创建圈子成功后自动创建社区聊天会话（ChatSession, Channel 类型），
/// 与圈子在同一 DbContext 事务内提交，客户端无需再单独调用创建会话接口。</para>
/// </summary>
public class CreateCircleCommandHandler(
    ICircleRepository circleRepository,
    IChatSessionRepository sessionRepository,
    IUserInfoRepository userInfoRepository,
    ILogger<CreateCircleCommandHandler> logger) : IRequestHandler<CreateCircleCommand, Guid>
{
    public async Task<Guid> Handler(CreateCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建圈子，圈主: {OwnerGuid}", command.UserId);

            // 设计文档 4.4：社区创建数量上限 = 用户等级（无资料按 1 级）
            var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
            var level = userInfo?.Level ?? 1;
            var ownedCount = (await circleRepository.GetByOwnerAsync(command.UserId)).Count();
            if (ownedCount >= level)
                throw new InvalidOperationException($"当前等级 {level} 最多可创建 {level} 个社区");

            var circle = Circle.Create(
                command.UserId,
                command.Name,
                command.Description,
                command.AvatarUrl,
                command.CoverUrl,
                command.MaxMembers ?? 500);

            await circleRepository.AddAsync(circle);

            // 联动：自动创建社区聊天会话（Channel，初始参与者 = 圈主；成员加入经事件同步）
            var session = ChatSession.CreateChannelSession(circle.CircleGuid, command.UserId, new HashSet<Guid>());
            await sessionRepository.AddAsync(session);

            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子创建成功，ID: {CircleGuid}，社区聊天会话: {SessionId}", circle.CircleGuid, session.SessionId);
            return circle.CircleGuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建圈子失败，圈主: {OwnerGuid}", command.UserId);
            throw;
        }
    }
}
