namespace Message.Web.API.Application.Commands.UserInfo;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>更新背景封面命令处理程序（upsert：用户资料不存在时创建）。</summary>
public class UpdateBackgroundCoverCommandHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<UpdateBackgroundCoverCommandHandler> logger) : IRequestHandler<UpdateBackgroundCoverCommand, bool>
{
    public async Task<bool> Handler(UpdateBackgroundCoverCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
        if (userInfo is null)
        {
            userInfo = UserInfoEntity.Create(command.UserId);
            await userInfoRepository.AddAsync(userInfo);
        }

        userInfo.UpdateBackgroundCover(command.BackgroundCoverUrl);
        await userInfoRepository.UpdateAsync(userInfo);
        await userInfoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 背景封面已更新", command.UserId);
        return true;
    }
}
