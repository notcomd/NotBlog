using Video.Domain.Entities;
using Video.Domain.Server;

namespace Video.Web.API.Apis;

public static class VideoCollectionEndpoints
{
    public static RouteGroupBuilder MapVideoCollectionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/videocollection")
            .WithTags("VideoCollection");

        group.MapGet("/", GetByVideoCollectionAsync)
            .WithName("GetVideoCollectionList")
            .WithDescription("Get all video collections");

        return group;
    }

    private static async Task<List<VideoCollection>> GetByVideoCollectionAsync(
        VideoCollectionService videoCollectionService)
    {
        return await videoCollectionService.GetByVideoCollectionListAsync();
    }
}
