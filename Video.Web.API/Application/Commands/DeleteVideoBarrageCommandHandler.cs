namespace Video.Web.API.Application.Commands;


public class DeleteVideoBarrageCommandHandler (IVideoRepository videoRepository): IRequestHandler<DeleteVideoBarrageCommand, bool>
{
    
    private readonly IVideoRepository _videoRepository=videoRepository?? throw new ArgumentNullException(nameof(videoRepository));


    public async Task<bool> Handler(DeleteVideoBarrageCommand request, CancellationToken cancellationToken)
    {
        var video = await _videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);
        if (video is null)
            return false;

        var barrage = video.VideoBarrageList?.FirstOrDefault(b => b.VideoBarrageGuid == request.VideoBarrageGuid);
        if (barrage is null)
            return false;

        barrage.SoftDelete();
        await _videoRepository.UpdateByVideoAsync(video);
        await _videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return true;
    }
}