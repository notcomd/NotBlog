using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IRepository;

namespace Video.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GetByVideoCollectionControllers : ControllerBase
{
    private readonly ILogger<IVideoCollectionRepository> _loggerVideoCollection;

    private readonly IVideoCollectionRepository _videoRepository;

    public GetByVideoCollectionControllers(IVideoCollectionRepository videoRepository,
        ILogger<IVideoCollectionRepository> loggerVideoCollection)
    {
        _videoRepository = videoRepository;
        _loggerVideoCollection = loggerVideoCollection;
    }

    [HttpGet]
    public async Task<List<VideoCollection>> GetByVideoCollectionAsync()
    {
        return await _videoRepository.FindByVideoCollectionListAsync();
    }
}