
namespace Identity.Web.API.Application.Command
{
    public abstract class IdentifiedCommandHandler<TCommand, TResult> : IRequestHandler<IdentifiedCommand<TCommand, TResult>, TResult>
       where TCommand : IRequest<TResult>
    {

        private readonly INotMediator _notMediator;
        private readonly IRequestManager _requestManager;
        private readonly ILogger<IdentifiedCommandHandler<TCommand, TResult>> _logger;

        protected IdentifiedCommandHandler(INotMediator notMediator, IRequestManager requestManager, ILogger<IdentifiedCommandHandler<TCommand, TResult>> logger)
        {
            _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
            _requestManager = requestManager ?? throw new ArgumentNullException(nameof(requestManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected abstract TResult CreateResultForDuplicateRequest();

        public async Task<TResult> Handler(IdentifiedCommand<TCommand, TResult> request, CancellationToken cancellationToken)
        {
            var alreadyExists = await _requestManager.ExistAsync(request.Id);
            if (alreadyExists)
            {
                _logger.LogInformation($"Request with id {request.Id} already exists.");
                return CreateResultForDuplicateRequest();
            }
            else
            {
                await _requestManager.CreateRequestForCommandAsync<TCommand>(request.Id);
                try
                {
                    var command = request.Command;
                    var commandName = command.GetGenericTypeName();
                    var idProperty = string.Empty;
                    var commandid = string.Empty;
                    switch(command)
                    {
                        case CreateByEmailUserCommand identifiedCommand:
                            idProperty = nameof(identifiedCommand.RoleName);
                            commandid = identifiedCommand.RoleName;
                            break;
                        case  CreateByPhoneUserCommand createByPhoneUserCommand:
                            idProperty = nameof(createByPhoneUserCommand.);
                            commandid = identifiedCommandGuid.CommandId.ToString();
                            break;
                        case IIdentifiedCommand<string> identifiedCommandString:
                            idProperty = identifiedCommandString.IdProperty;
                            commandid = identifiedCommandString.CommandId;
                            break;
                        default:
                            idProperty = "Unknown";
                            commandid = "Unknown";
                            break;
                    }
                }
                catch
                {
                    return default;
                }
            }
            await _requestManager.CreateRequestForCommandAsync<TCommand>(request.Id);
        }
    }
}
