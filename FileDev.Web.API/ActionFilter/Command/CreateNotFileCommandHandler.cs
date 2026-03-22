using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using FileDev.Infrastructure.Idempotent;
using Microsoft.Extensions.Options;
using NotMediator;

namespace FileDev.Web.API.ActionFilter.Command;

public class CreateNotFileCommandHandler(
    INotFileStorageService storageProvider,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    INotFileService notFileService,
    INotFileGroupService notFileGroupService)
    : IRequestHandler<CreateNotFileCommand, bool>
{
    private readonly IOptionsSnapshot<NotFileStorageOptions> _configOptions =
        configOptions ?? throw new ArgumentNullException(nameof(configOptions));

    private readonly INotFileGroupService _notFileGroupService =
        notFileGroupService ?? throw new ArgumentNullException(nameof(notFileGroupService));

    private readonly INotFileService _notFileService =
        notFileService ?? throw new ArgumentNullException(nameof(notFileService));

    private readonly INotFileStorageService _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));


    public async Task<bool> Handler(CreateNotFileCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


    public class CreateNotFileIdentifiedCommandHandler<T>(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<IdentifiedCommandHandler<CreateNotFileCommand, bool>> logger)
        : IdentifiedCommandHandler<CreateNotFileCommand, bool>(mediator, requestManagement, logger)
    {
        protected override bool CreateResultForDuplicateRequest()
        {
            return true;
        }
    }
}