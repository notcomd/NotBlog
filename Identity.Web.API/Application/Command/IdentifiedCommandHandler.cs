
namespace Identity.Web.API.Application.Command
{
    public abstract class IdentifiedCommandHandler<TCommand, TResult> :IRequestHandler<IdentifiedCommand<TCommand, TResult>, TResult>
       where TCommand : IRequest<TResult>
    {
        public Task<TResult> Handler(IdentifiedCommand<TCommand, TResult> request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
