using Message.Domain.Entities;
using Message.Domain.Entities.Group;
using Message.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using NotMediator;
using MessageEntity = Message.Domain.Entities.Message;

namespace Message.Infrastructure.EntityFramework;

public class MessageDbContext : DbContext, IUnitOfWork
{
    private readonly INotMediator _notMediator;

    public MessageDbContext(DbContextOptions<MessageDbContext> options, INotMediator notMediator) : base(options)
    {
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
    }


    public DbSet<MessageEntity> Messages { get; set; } = null!;

    public DbSet<ChatSession> ChatSessions { get; set; } = null!;

    public DbSet<MessageFriends> MessageFriends { get; set; } = null!;

    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;

    public DbSet<FileAttachment> FileAttachments { get; set; } = null!;

    public IUnitOfWork UnitOfWork => this;

    public async Task<int> SavaChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessageDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}