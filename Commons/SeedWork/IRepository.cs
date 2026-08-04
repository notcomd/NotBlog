namespace Commons.SeedWork;

public interface IRepository<T, TUow>
    where T : IAggregateRoot
    where TUow : IUnitOfWork
{
    TUow UnitOfWork { get; }
}
