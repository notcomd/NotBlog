namespace Message.Web.API.Application.Commands.Community;
/// <summary>转移圈主命令处理程序。</summary>
public class TransferCircleOwnershipCommandHandler(
    ICircleRepository circleRepository,
    ILogger<TransferCircleOwnershipCommandHandler> logger) : IRequestHandler<TransferCircleOwnershipCommand, bool>
{
    public async Task<bool> Handler(TransferCircleOwnershipCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdWithMembersAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            circle.TransferOwnership(command.NewOwnerGuid, command.OperatorGuid);

            await circleRepository.UpdateAsync(circle);
            await circleRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("圈主已转移: Circle={CircleGuid}, NewOwner={NewOwnerGuid}",
                command.CircleGuid, command.NewOwnerGuid);
            return true;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not InvalidOperationException)
        {
            logger.LogError(ex, "转移圈主失败: Circle={CircleGuid}", command.CircleGuid);
            throw;
        }
    }
}
