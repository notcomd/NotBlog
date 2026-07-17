namespace FileDev.Web.API.Application.Command;

public class CreateNotFileGroupCommandHandler(
    INotFileStorageService storageProvider,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    INotFileService notFileService,
    INotFileGroupService notFileGroupService)
    : NotMediator.IRequestHandler<CreateNotFileGroupCommand, bool>
{
    private readonly INotFileGroupService _notFileGroupService =
        notFileGroupService ?? throw new ArgumentNullException(nameof(notFileGroupService));

    public async Task<bool> Handler(CreateNotFileGroupCommand request, CancellationToken cancellationToken)
    {
        if (request.UserGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileGroupName))
            throw new ArgumentException("文件组名称不能为空");

        await _notFileGroupService.CreateNotFileGroupAsync(
            request.UserGuid,
            request.FileGroupName,
            request.FileGroupDescription,
            request.FileGroupTags,
            request.FileIdentity);

        return true;
    }

    public class CreateNotFileGroupIdentifiedCommandHandler(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<CreateNotFileGroupIdentifiedCommandHandler> logger)
        : IdentifiedCommandHandler<CreateNotFileGroupCommand, bool>(mediator, requestManagement, logger)
    {
        protected override bool CreateResultForDuplicateRequest() => true;
    }
}
