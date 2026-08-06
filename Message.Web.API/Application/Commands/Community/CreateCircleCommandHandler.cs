
namespace Message.Web.API.Application.Commands.Community;
/// <summary>创建圈子命令处理程序。</summary>
public class CreateCircleCommandHandler(
    ICircleRepository circleRepository,
    ILogger<CreateCircleCommandHandler> logger) : IRequestHandler<CreateCircleCommand, Guid>
{
    public async Task<Guid> Handler(CreateCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建圈子，圈主: {OwnerGuid}", command.UserId);

            var circle = Circle.Create(
                command.UserId,
                command.Name,
                command.Description,
                command.AvatarUrl,
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
