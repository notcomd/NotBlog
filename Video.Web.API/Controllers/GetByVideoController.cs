using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.IRepository;
using Video.Domain.Server;

namespace Video.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GetByVideoController : ControllerBase
{

    private readonly ILogger<IVideoRepository> _loggerVideoService;

    private readonly VideoService _videoRepository;


    public GetByVideoController(VideoService videoRepository, ILogger<IVideoRepository> loggerVideoService)
    {
        _videoRepository = videoRepository;
        _loggerVideoService = loggerVideoService;
    }

    [HttpGet]
    public async Task<IVideoResult<List<Videos>>> GetByVideoListAsync()
    {
        var videoModel = await _videoRepository.GetByVideosAllAsync();
        return new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "成功", videoModel);
    }

    [HttpGet("{index:int}/{pageSize:int}")]
    public async Task<IVideoResult<List<Videos>>> GetByVideoPage(int index, int pageSize)
    {
        var videoModel = await _videoRepository.PagesByVideosAsync(index, pageSize);
        return new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "成功", videoModel);
    }



    [HttpGet("Findname/{videoName}")]
    public async Task<IVideoResult<Videos>> GetByVideoAsync(string videoName)
    {
        var videoModel = await _videoRepository.GetByVideoAsync(videoName);
        return new IVideoResult<Videos>(VideoResultType.VideoResultOk, 200, "成功", videoModel);
    }

    [HttpGet("Blurred/{videoName}")]
    public async Task<IVideoResult<List<Videos>>> BlurredByVideoAsync(string videoName)
    {
        var videoModel = await _videoRepository.BlurredByVideoAsync(videoName);
        return new IVideoResult<List<Videos>>(VideoResultType.VideoResultOk, 200, "成功", videoModel);
    }
}