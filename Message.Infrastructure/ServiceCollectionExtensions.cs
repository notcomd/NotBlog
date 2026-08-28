
using Message.Infrastructure.MongoMigration;
using Message.Infrastructure.Services;

namespace Message.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        // F-04: DB registration is unified at host (Program.cs AddNpgsql<MessageDbContext>).
        // Do not re-register DbContext here; the old UseSqlServer registration overrode the host Npgsql one.

        RegisterRepositories(services);
        RegisterServices(services, configuration);

        // EventBus（RabbitMQ）注册已移至宿主 Program.cs（AddEventBus 需扫描 Web.API 程序集的
        // 集成事件消费者，且 RabbitMqEventBus 为 Singleton 非 Try 注册、不可重复调用）。
        // IConnectionFactory 由宿主注册（DEBUG：appsettings EventBus 节手动 ConnectionFactory；Release：Aspire AddRabbitMQClient("EventBus")）。

        return services;
    }

    public static IServiceCollection AddMessageInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<MessageDbContext>(optionsAction);

        RegisterRepositories(services);
        RegisterServices(services);

        return services;
    }

    public static IServiceCollection AddInMemoryMessageInfrastructure(
        this IServiceCollection services)
    {
        services.AddDbContext<MessageDbContext>(options => { options.UseInMemoryDatabase("MessageDb"); });

        RegisterRepositories(services);
        RegisterServices(services);

        return services;
    }

    /// <summary>
    /// 以 Mongo 仓储覆盖消息/会话仓储注册（D2-1：message + chat_session 集合切换到 Mongo）。
    /// <para>必须在 <see cref="RegisterRepositories"/> 之后调用——AddScoped 后注册者胜出；
    /// 社交域（FileAttachment/群/好友/Tweet）仍保留 EF 注册。测试用的内存库不走此路径。</para>
    /// </summary>
    public static IServiceCollection AddMessageMongoRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMessageRepository, MongoMessageRepository>();
        services.AddScoped<IChatSessionRepository, MongoChatSessionRepository>();
        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IMessageFriendsRepository, MessageFriendsRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();

        services.AddScoped<ITweetRepository, TweetRepository>();
        services.AddScoped<ITweetInteractionRepository, TweetInteractionRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITweetAuditRepository, TweetAuditRepository>();
        services.AddScoped<ITweetReportRepository, TweetReportRepository>();
        services.AddScoped<ITweetNotificationRepository, TweetNotificationRepository>();

        // 兴趣社区（Community）：圈子 / 邀请 / 话题 / 关注
        services.AddScoped<ICircleRepository, CircleRepository>();
        services.AddScoped<ICircleInvitationRepository, CircleInvitationRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IUserFollowRepository, UserFollowRepository>();

        // 用户资料（UserInfo）：等级 / 经验 / 硬币 / 背景封面 + 签到记录
        services.AddScoped<IUserInfoRepository, UserInfoRepository>();
        services.AddScoped<IUserSignInRepository, UserSignInRepository>();
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        // CQRS：连接管理按职责拆分注册——查询侧（IConnectionManager）与命令侧（IConnectionCommandService）指向同一实现
        services.AddScoped<IConnectionManager, RedisConnectionManager>();
        services.AddScoped<IConnectionCommandService, RedisConnectionManager>();


        services.AddScoped<ISensitiveWordFilter, DefaultSensitiveWordFilter>();
        services.AddScoped<IImageModerationService, DefaultImageModerationService>();
        services.AddScoped<IEmailSender, DefaultEmailSender>();
        services.AddScoped<ILocalizationService, DefaultLocalizationService>();
        services.AddSingleton<IMessageRecallPolicy, DefaultMessageRecallPolicy>();

        services.TryAddSingleton<MessageCacheService>();
        services.TryAddSingleton<SessionCacheService>();
        services.TryAddSingleton<UnreadCountCacheService>();
        services.TryAddSingleton<UserStatusCacheService>();

        // 存量聊天数据迁移（PG → Mongo），一次性后台任务，由宿主按配置门控触发。
        services.AddScoped<MessageMongoMigrationService>();

        // 社区事件发布器（AI 机器人 / MCP 扩展出口）：
        // CommunityEventBus:Enabled = true → RabbitMQ 实现（需宿主已注册 IConnectionFactory）；
        // 默认 → No-op 实现（仅日志），保证零外部依赖可运行。
        if (configuration?.GetValue<bool>("CommunityEventBus:Enabled") == true)
            services.AddScoped<ICommunityEventPublisher, RabbitMqCommunityEventPublisher>();
        else
            services.AddScoped<ICommunityEventPublisher, DefaultCommunityEventPublisher>();
    }
}