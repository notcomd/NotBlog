namespace FileDev.Web.API.Application.Command;

/// <summary>
/// 幂等命令处理器：包装 <see cref="CreateNotFileCommand"/>，
/// 通过 <see cref="IdentifiedCommand{T,R}"/> 标识符去重，防止客户端重试导致重复创建。
/// </summary>
public class CreateNotFileIdentifiedCommandHandler(
    INotMediator mediator,
    IRequestManagement requestManagement,
    ILogger<CreateNotFileIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<CreateNotFileCommand, bool>(mediator, requestManagement, logger)
{
    protected override bool CreateResultForDuplicateRequest() => true;
}
