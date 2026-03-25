using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Message.Infrastructure.Provider;
using Message.Infrastructure.Repository;
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

        return services;
    }

    public static IServiceCollection AddMessageInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<MessageDbContext>(optionsAction);

        RegisterRepositories(services);
        RegisterProviders(services);

        return services;
    }

    public static IServiceCollection AddInMemoryMessageInfrastructure(
        this IServiceCollection services)
    {
        services.AddDbContext<MessageDbContext>(options => { options.UseInMemoryDatabase("MessageDb"); });

        RegisterRepositories(services);
        RegisterProviders(services);

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IMessageFriendsRepository, MessageFriendsRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
    }

    private static void RegisterProviders(IServiceCollection services)
    {
        services.AddScoped<IUserProvider, UserProvider>();
        services.AddScoped<IMessageProvider, MessageProvider>();
        services.AddScoped<IChatSessionProvider, ChatSessionProvider>();
        services.AddScoped<IFriendProvider, FriendProvider>();
        services.AddScoped<IGroupProvider, GroupProvider>();
        services.AddScoped<IFileProvider, FileProvider>();
    }
}