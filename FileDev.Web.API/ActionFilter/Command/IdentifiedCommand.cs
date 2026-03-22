using NotMediator;

namespace FileDev.Web.API.ActionFilter.Command;

public class IdentifiedCommand<T, R>(Guid id, T command) : IRequest<R>
{
    public Guid Id { get; set; } = id;

    public T Command { get; set; } = command;
}