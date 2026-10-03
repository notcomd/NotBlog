using MessageEntity = Message.Domain.Entities.Chat.Message;
using Message.Domain.Entities.Announcement;

namespace Message.Infrastructure.EntityFramework;

/// <summary>
/// 消息模块的 Entity Framework Core 数据库上下文，负责各实体集的访问与持久化，并实现工作单元以统一提交与分发领域事件。
/// </summary>
public class MessageDbContext : DbContext, IUnitOfWork
{
    private readonly INotMediator _notMediator;

    /// <summary>
    /// 初始化 <see cref="MessageDbContext"/> 实例。
    /// </summary>
    /// <param name="options">数据库上下文配置选项。</param>
    /// <param name="notMediator">用于分发领域事件的中介者。</param>
    public MessageDbContext(DbContextOptions<MessageDbContext> options, INotMediator notMediator) : base(options)
    {
        _notMediator = notMediator ?? throw new ArgumentNullException(nameof(notMediator));
    }


    /// <summary>
    /// 获取或设置消息实体集（Messages 表）。
    /// </summary>
    public DbSet<MessageEntity> Messages { get; set; } = null!;

    /// <summary>
    /// 获取或设置聊天会话实体集（ChatSessions 表）。
    /// </summary>
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;

    /// <summary>
    /// 获取或设置好友关系实体集（MessageFriends 表）。
    /// </summary>
    public DbSet<MessageFriends> MessageFriends { get; set; } = null!;

    /// <summary>
    /// 获取或设置群组实体集（Groups 表）。
    /// </summary>
    public DbSet<Group> Groups { get; set; } = null!;
    /// <summary>
    /// 获取或设置群成员实体集（GroupMembers 表）。
    /// </summary>
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;

    /// <summary>
    /// 获取或设置文件附件实体集（FileAttachments 表）。
    /// </summary>
    public DbSet<FileAttachment> FileAttachments { get; set; } = null!;

    /// <summary>
    /// 获取或设置动态（推文）实体集（Tweets 表）。
    /// </summary>
    public DbSet<Tweet> Tweets { get; set; } = null!;
    /// <summary>
    /// 获取或设置评论实体集（Comments 表）。
    /// </summary>
    public DbSet<Comment> Comments { get; set; } = null!;
    /// <summary>
    /// 获取或设置动态互动（点赞、收藏等）实体集（TweetInteractions 表）。
    /// </summary>
    public DbSet<TweetInteraction> TweetInteractions { get; set; } = null!;
    /// <summary>
    /// 获取或设置动态审核日志实体集（TweetAuditLogs 表）。
    /// </summary>
    public DbSet<TweetAuditLog> TweetAuditLogs { get; set; } = null!;
    /// <summary>
    /// 获取或设置动态举报实体集（TweetReports 表）。
    /// </summary>
    public DbSet<TweetReport> TweetReports { get; set; } = null!;

    /// <summary>
    /// 获取或设置动态通知实体集（TweetNotifications 表）。
    /// </summary>
    public DbSet<TweetNotification> TweetNotifications { get; set; } = null!;

    /// <summary>
    /// 获取或设置圈子实体集（Circles 表）。
    /// </summary>
    public DbSet<Circle> Circles { get; set; } = null!;
    /// <summary>
    /// 获取或设置圈子成员实体集（CircleMembers 表）。
    /// </summary>
    public DbSet<CircleMember> CircleMembers { get; set; } = null!;
    /// <summary>
    /// 获取或设置圈子邀请实体集（CircleInvitations 表）。
    /// </summary>
    public DbSet<CircleInvitation> CircleInvitations { get; set; } = null!;
    /// <summary>
    /// 获取或设置话题实体集（Topics 表）。
    /// </summary>
    public DbSet<Topic> Topics { get; set; } = null!;
    /// <summary>
    /// 获取或设置用户关注关系实体集（UserFollows 表）。
    /// </summary>
    public DbSet<UserFollow> UserFollows { get; set; } = null!;
    /// <summary>
    /// 获取或设置用户信息实体集（UserInfos 表）。
    /// </summary>
    public DbSet<UserInfo> UserInfos { get; set; } = null!;
    /// <summary>
    /// 获取或设置用户签到实体集（UserSignIns 表）。
    /// </summary>
    public DbSet<UserSignIn> UserSignIns { get; set; } = null!;

    /// <summary>
    /// 获取或设置公告实体集（Announcements 表）。
    /// </summary>
    public DbSet<Announcement> Announcements { get; set; } = null!;


    /// <summary>
    /// 获取当前上下文作为工作单元实例。
    /// </summary>
    public IUnitOfWork UnitOfWork => this;

    /// <summary>
    /// 分发已发生的领域事件并保存所有更改，返回是否保存成功。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>保存成功返回 true。</returns>
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 配置实体模型，从当前程序集加载并应用所有实体配置。
    /// </summary>
    /// <param name="modelBuilder">用于构建模型的构建器。</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessageDbContext).Assembly);
        
    }

    /// <summary>
    /// 分发已发生的领域事件并异步保存所有更改。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写入数据库的状态条目数量。</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _notMediator.DispatchDomainEventsAsync(this, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}