using Microsoft.Extensions.DependencyInjection;
using Video.Domain.IRepository;
using Video.Domain.Server;
using Video.Infrastructure.Repository;
using Video.Infrastructure.Service;

namespace Video.Infrastructure;

public static class VideoInfrastructureExtensions
{
    public static IServiceCollection AddVideoInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IVideoCollectionRepository, VideoCollectionRepository>();

        // Domain Services
        services.AddScoped<VideoService>();
        services.AddScoped<VideoCollectionService>();

        // Infrastructure Services
        services.AddHttpClient<FileDevClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000"); // FileDev.Web.API base URL
            client.Timeout = TimeSpan.FromMinutes(30); // Long timeout for video uploads
        });

        return services;
    }

    public static IServiceCollection AddVideoInfrastructure(
        this IServiceCollection services, string fileDevBaseUrl)
    {
        services.AddVideoInfrastructure();

        services.AddHttpClient<FileDevClient>(client =>
        {
            client.BaseAddress = new Uri(fileDevBaseUrl);
            client.Timeout = TimeSpan.FromMinutes(30);
        });

        return services;
    }
}
