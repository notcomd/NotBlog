
namespace Message.Web.API.Application.Commands.Community;
/// <summary>创建圈子命令处理程序。</summary>
public class CreateCircleCommandHandler(
    ICircleRepository circleRepository,
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
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子创建成功，ID: {CircleGuid}", circle.CircleGuid);
            return circle.CircleGuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建圈子失败，圈主: {OwnerGuid}", command.UserId);
            throw;
        }
    }
}
