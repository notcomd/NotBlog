namespace Identity.Domain.IRepository;

public interface IWorkAsyncFilter
{
    public virtual ValueTask WorkValueTask()
    {
        return ValueTask.CompletedTask;
    }

}