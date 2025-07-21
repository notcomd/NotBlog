
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

        public Task<TResult> Handler(IdentifiedCommand<TCommand, TResult> request, CancellationToken cancellationToken)
        {

        }
    }
}
