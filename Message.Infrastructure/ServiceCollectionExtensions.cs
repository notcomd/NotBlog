
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

        // 社区事件总线（RabbitMQ）：由 CommunityEventBus:Enabled 控制。
        // IConnectionFactory 由宿主注册（DEBUG：appsettings EventBus 节手动 ConnectionFactory；Release：Aspire AddRabbitMQClient("EventBus")）。
        if (configuration.GetValue<bool>("CommunityEventBus:Enabled"))
        {
            services.AddEventBus(configuration.GetSection("EventBus"), Array.Empty<System.Reflection.Assembly>());
        }

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

        services.TryAddSingleton<RedisCacheService>();
        services.TryAddSingleton<SessionCacheService>();
        services.TryAddSingleton<UnreadCountCacheService>();
        services.TryAddSingleton<UserStatusCacheService>();

        // 社区事件发布器（AI 机器人 / MCP 扩展出口）：
        // CommunityEventBus:Enabled = true → RabbitMQ 实现（需宿主已注册 IConnectionFactory）；
        // 默认 → No-op 实现（仅日志），保证零外部依赖可运行。
        if (configuration?.GetValue<bool>("CommunityEventBus:Enabled") == true)
            services.AddScoped<ICommunityEventPublisher, RabbitMqCommunityEventPublisher>();
        else
            services.AddScoped<ICommunityEventPublisher, DefaultCommunityEventPublisher>();
    }
}