using Microsoft.AspNetCore.Mvc;
using Video.Web.API.Dto.Request;

namespace Video.Web.API.Apis;

[ApiController]
[Route("api/[controller]")]
public class VideoReviewController : ControllerBase
{
    [HttpPost("AddVideoReview")]
    public Task<string> AddVideoReview([FromBody] RequestAddReview request)
    {
        var str = request.ToString();
        return Task.FromResult(str);
    }
}