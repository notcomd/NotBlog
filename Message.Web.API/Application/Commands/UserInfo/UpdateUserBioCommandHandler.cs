namespace Message.Web.API.Application.Commands.UserInfo;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>更新个人签名命令处理程序（upsert：用户资料不存在时创建）。</summary>
public class UpdateUserBioCommandHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<UpdateUserBioCommandHandler> logger) : IRequestHandler<UpdateUserBioCommand, bool>
{
    public async Task<bool> Handler(UpdateUserBioCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
        if (userInfo is null)
        {
            userInfo = UserInfoEntity.Create(command.UserId);
            await userInfoRepository.AddAsync(userInfo);
        }

        userInfo.UpdateBio(command.Bio);
        await userInfoRepository.UpdateAsync(userInfo);
        await userInfoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 个人签名已更新", command.UserId);
        return true;
    }
}