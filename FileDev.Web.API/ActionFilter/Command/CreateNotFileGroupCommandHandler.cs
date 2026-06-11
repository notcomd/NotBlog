namespace FileDev.Web.API.ActionFilter.Command;

public class CreateNotFileGroupCommandHandler(
    INotFileStorageService storageProvider,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    INotFileService notFileService,
    INotFileGroupService notFileGroupService)
    : NotMediator.IRequestHandler<CreateNotFileGroupCommand, bool>
{
    private readonly IOptionsSnapshot<NotFileStorageOptions> _configOptions =
        configOptions ?? throw new ArgumentNullException(nameof(configOptions));

    private readonly INotFileGroupService _notFileGroupService =
        notFileGroupService ?? throw new ArgumentNullException(nameof(notFileGroupService));

    private readonly INotFileService _notFileService =
        notFileService ?? throw new ArgumentNullException(nameof(notFileService));

    private readonly INotFileStorageService _notFileStorageService =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));


    public async Task<bool> Handler(CreateNotFileGroupCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public class CreateNotFileGroupIdentifiedCommandHandler(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<CreateNotFileGroupIdentifiedCommandHandler> logger)
        : IdentifiedCommandHandler<CreateNotFileGroupCommand, bool>(mediator, requestManagement, logger)
    {
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}