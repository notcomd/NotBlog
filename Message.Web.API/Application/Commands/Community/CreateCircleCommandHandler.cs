
namespace Message.Web.API.Application.Commands.Community;
/// <summary>
/// 创建圈子命令处理程序。
/// <para>联动：创建圈子成功后自动创建社区群组（Group，CircleId 关联社区、圈主即群主）
/// 与群组会话（ChatSession，SessionType.Group），与圈子在同一 DbContext 事务内提交，
/// 客户端无需再单独调用创建群组/会话接口。</para>
/// </summary>
public class CreateCircleCommandHandler(
    ICircleRepository circleRepository,
    IGroupRepository groupRepository,
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

            // 联动：社区聊天以真群组承载（圈主随 Group 构造写入 GroupMember；成员加入/退出经领域事件同步）
            var group = Group.CreateForCircle(circle.CircleGuid, circle.OwnerGuid, circle.Name, circle.MaxMembers);
            await groupRepository.AddAsync(group);

            // 联动：群组会话（SessionType.Group），初始参与者 = 圈主；CircleId 供社区聊天入口按社区解析会话
            var session = ChatSession.CreateCommunityGroupSession(
                group.GroupId, circle.CircleGuid, command.UserId, new HashSet<Guid>());
            await sessionRepository.AddAsync(session);

            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子创建成功，ID: {CircleGuid}，社区群组: {GroupId}，社区聊天会话: {SessionId}",
                circle.CircleGuid, group.GroupId, session.SessionId);
            return circle.CircleGuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建圈子失败，圈主: {OwnerGuid}", command.UserId);
            throw;
        }
    }
}
