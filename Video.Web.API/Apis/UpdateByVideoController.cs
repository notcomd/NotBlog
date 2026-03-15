using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Video.Domain.Entities;
using Video.Domain.Server;
using Video.Web.API.Dto.Request;

namespace Video.Web.API.Apis;

[ApiController]
[Route("api/[controller]")]
public class UpdateByVideoController(VideoService videoRepository, ILogger<VideoService> loggerVideoService)
    :ControllerBase
{
    /// <summary>
    ///     管理员无法更新视频信息
    /// </summary>
    /// <param name="updateVideo"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    [Authorize]
    [HttpPut]
    public async Task<IVideoResult<string>> UpdateByVideoAsync([FromBody] RequestUpdateByVideo updateVideo)
    {
        if (updateVideo is null)
        {
            loggerVideoService.LogError("updateVideo is null");
            throw new ArgumentNullException(nameof(updateVideo));
        }

        var videoModel = await videoRepository.GetByVideoAsync(updateVideo.VideoGuid);
        //发送一个消息-我要更新数据了 {AffiliatedUserGuid}

        if (videoModel.VideoGuid != updateVideo.AffiliatedUserGuid)
            return new IVideoResult<string>(VideoResultType.VideoResultUnauthorized, 403, "你正在越权删除他人数据",
                "Warring 请不要这么做！");
        var model = new Videos(videoModel.Affiliated, updateVideo.VideoName, updateVideo.VideoCover,
            updateVideo.VideoCover,
            updateVideo.BriefIntroduction, updateVideo.Tags);
        await videoRepository.UpdateByVideoAsync(model);

        return new IVideoResult<string>(VideoResultType.VideoResultOk, 200, "更新成功", "UP!");
    }


    // [HttpPut("protected")]
    // public async Task<IVideoResult<string>> UpDateByControlAsync(DtoByUpControl updateVideo)
    // {
    //     var videoModel = await _videoService.GetByVideoAsync(updateVideo.VideoGuid);
    //
    //     var model = new Videos(videoModel.Affiliated, videoModel.VideoName, videoModel.VideoCover,
    //         videoModel.VideoFileUri, videoModel.BriefIntroduction, videoModel.VideoTags);
    //
    //     model.VideoControl.Author(AuthorVideo.VideoProtected);
    //
    //     if (model.VideoControl.GetAuthorVideo() == AuthorVideo.VideoProtected)
    //     {
    //         model.VideoControl.SetProtectedTime(updateVideo.StartTime, updateVideo.EndTime);
    //         _loggerVideoService.LogInformation("设置视频保护时间成功");
    //     }
    //
    //     if (model.VideoControl.GetAuthorVideo() == AuthorVideo.VideoPrivate)
    //     {
    //         model.VideoControl.Display(true);
    //         _loggerVideoService.LogInformation("设置视频私有成功");
    //     }
    //
    //     return new IVideoResult<string>(VideoResultType.VideoResultOk, 200, "更新成功", "UP!");
    // }
}