namespace Message.Web.API.Application.Commands.UserInfo;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>增加硬币命令处理程序（upsert：用户资料不存在时创建）。</summary>
public class AddUserCoinsCommandHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<AddUserCoinsCommandHandler> logger) : IRequestHandler<AddUserCoinsCommand, bool>
{
    public async Task<bool> Handler(AddUserCoinsCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
        if (userInfo is null)
        {
            userInfo = UserInfoEntity.Create(command.UserId);
            await userInfoRepository.AddAsync(userInfo);
        }

        userInfo.AddCoins(command.Amount);
        await userInfoRepository.UpdateAsync(userInfo);
        await userInfoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 增加硬币 {Amount}，当前余额 {Coins}", command.UserId, command.Amount, userInfo.Coins);
        return true;
    }
}
