using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Repository;
using Message.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        RegisterServices(services);

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
    }

    private static void RegisterServices(IServiceCollection services)
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
    }
}