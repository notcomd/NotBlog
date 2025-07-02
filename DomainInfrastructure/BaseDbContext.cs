using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Notcomd.DomainCommand;

public abstract class BaseDbContext : DbContext
{

    private IMediator? _mediator;

    public BaseDbContext(DbContextOptions options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotImplementedException("Don not call SaveChanges, please call SaveChangesAsync instead.");
    }

    public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new())
    {
        if (_mediator != null)
        {
            await _mediator.DispatchDomainEventsAsync(this);
        }

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        return result;
    }
}