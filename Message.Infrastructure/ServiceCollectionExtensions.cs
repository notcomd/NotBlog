using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Provider;
using Message.Infrastructure.Repository;
using Message.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Message.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        services.AddDbContext<MessageDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MessageDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        RegisterRepositories(services);
        RegisterProviders(services);
        RegisterServices(services);

        return services;
    }

    public static IServiceCollection AddMessageInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<MessageDbContext>(optionsAction);

        RegisterRepositories(services);
        RegisterProviders(services);
        RegisterServices(services);

        return services;
    }

    public static IServiceCollection AddInMemoryMessageInfrastructure(
        this IServiceCollection services)
    {
        services.AddDbContext<MessageDbContext>(options => { options.UseInMemoryDatabase("MessageDb"); });

        RegisterRepositories(services);
        RegisterProviders(services);
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
    }

    private static void RegisterProviders(IServiceCollection services)
    {
        services.AddScoped<IMessageProvider, MessageProvider>();
        services.AddScoped<IChatSessionProvider, ChatSessionProvider>();
        services.AddScoped<IFriendProvider, FriendProvider>();
        services.AddScoped<IGroupProvider, GroupProvider>();
        services.AddScoped<IFileProvider, FileProvider>();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IConnectionManager, RedisConnectionManager>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IUserStatusCacheService, UserStatusCacheService>();
        services.AddScoped<ISessionCacheService, SessionCacheService>();
        services.AddScoped<IUnreadCountCacheService, UnreadCountCacheService>();
    }
}