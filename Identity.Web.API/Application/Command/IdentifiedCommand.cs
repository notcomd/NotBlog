namespace Identity.Web.API.Application.Command;

public class IdentifiedCommand<T, R>(Guid id, T command) : IRequest<R>
    where T : IRequest<R>
{
    public Guid Id { get; set; } = id;

    public T Command { get; set; } = command;
}