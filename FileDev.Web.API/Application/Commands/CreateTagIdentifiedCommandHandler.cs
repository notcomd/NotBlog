namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 幂等命令处理器：包装 <see cref="CreateTagCommand"/>，
/// 通过 <see cref="IdentifiedCommand{T,R}"/> 标识符去重，防止客户端重试导致重复创建标签。
/// </summary>
public class CreateTagIdentifiedCommandHandler(
    INotMediator mediator,
    IRequestManagement requestManagement,
    ILogger<CreateTagIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<CreateTagCommand, bool>(mediator, requestManagement, logger)
{
    protected override bool CreateResultForDuplicateRequest() => true;
}