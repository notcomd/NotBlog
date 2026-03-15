using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IRepository;

namespace Video.Web.API.Apis;

[ApiController]
[Route("api/[controller]")]
public class GetByVideoCollectionControllers(
    IVideoCollectionRepository videoRepository,
    ILogger<IVideoCollectionRepository> loggerVideoCollection)
    :ControllerBase
{
    private readonly ILogger<IVideoCollectionRepository> _loggerVideoCollection = loggerVideoCollection;

    [HttpGet]
    public async Task<List<VideoCollection>> GetByVideoCollectionAsync()
    {
        return await videoRepository.FindByVideoCollectionListAsync();
    }
}