namespace Message.Web.API.Application.Commands.Community;
/// <summary>解散圈子命令处理程序。</summary>
public class DissolveCircleCommandHandler(
    ICircleRepository circleRepository,
    ILogger<DissolveCircleCommandHandler> logger) : IRequestHandler<DissolveCircleCommand, bool>
{
    public async Task<bool> Handler(DissolveCircleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            if (circle.OwnerGuid != command.OperatorGuid)
                throw new UnauthorizedAccessException("只有圈主可以解散圈子");

            circle.Dissolve();

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈子已解散: {CircleGuid}", command.CircleGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            logger.LogError(ex, "解散圈子失败: {CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
