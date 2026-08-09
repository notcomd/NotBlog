using MessageEntity = Message.Domain.Entities.Chat.Message;

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

    public DbSet<Tweet> Tweets { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<TweetInteraction> TweetInteractions { get; set; } = null!;
    public DbSet<TweetAuditLog> TweetAuditLogs { get; set; } = null!;
    public DbSet<TweetReport> TweetReports { get; set; } = null!;

    public DbSet<TweetNotification> TweetNotifications { get; set; } = null!;

    public DbSet<Circle> Circles { get; set; } = null!;
    public DbSet<CircleMember> CircleMembers { get; set; } = null!;
    public DbSet<CircleInvitation> CircleInvitations { get; set; } = null!;
    public DbSet<Topic> Topics { get; set; } = null!;
    public DbSet<UserFollow> UserFollows { get; set; } = null!;
    public DbSet<UserInfo> UserInfos { get; set; } = null!;
    public DbSet<UserSignIn> UserSignIns { get; set; } = null!;


    public IUnitOfWork UnitOfWork => this;

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
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