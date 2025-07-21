namespace Identity.Web.API.Application.Command;

public class IdentifiedCommand<TCommand, Result> : IRequest<Result> where TCommand : IRequest<Result>
{

    public TCommand Command { get; }
    public Guid Id { get; }
    public IdentifiedCommand(TCommand command, Guid Id)
    {
        Command = command;
        this.Id = Id;
    }
}