using Identity.Domain.IRepository;

namespace Identity.Infrastructure.Repository;

public class DbContextMigrations : IWorkAsyncFilter
{
    public ValueTask WorkValueTask()
    {

        return ValueTask.CompletedTask;
    }
}