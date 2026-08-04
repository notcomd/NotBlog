namespace FileDev.Web.API.Application.Command;

/// <summary>
/// 幂等命令处理器：包装 <see cref="CreateNotFileGroupCommand"/>，
/// 通过 <see cref="IdentifiedCommand{T,R}"/> 标识符去重，防止客户端重试导致重复创建文件组。
/// </summary>
public class CreateNotFileGroupIdentifiedCommandHandler(
    INotMediator mediator,
    IRequestManagement requestManagement,
    ILogger<CreateNotFileGroupIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<CreateNotFileGroupCommand, bool>(mediator, requestManagement, logger)
{
    protected override bool CreateResultForDuplicateRequest() => true;
}
