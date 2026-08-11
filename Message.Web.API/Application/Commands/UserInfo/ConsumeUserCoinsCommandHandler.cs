namespace Message.Web.API.Application.Commands.UserInfo;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>扣除硬币命令处理程序（upsert：用户资料不存在时按余额 0 处理，必然余额不足）。</summary>
public class ConsumeUserCoinsCommandHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<ConsumeUserCoinsCommandHandler> logger) : IRequestHandler<ConsumeUserCoinsCommand, bool>
{
    public async Task<bool> Handler(ConsumeUserCoinsCommand command, CancellationToken cancellationToken)
    {
        var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
        if (userInfo is null)
        {
            userInfo = UserInfoEntity.Create(command.UserId);
            await userInfoRepository.AddAsync(userInfo);
        }

        userInfo.ConsumeCoins(command.Amount);
        await userInfoRepository.UpdateAsync(userInfo);
        await userInfoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("用户 {UserId} 扣除硬币 {Amount}，当前余额 {Coins}", command.UserId, command.Amount, userInfo.Coins);
        return true;
    }
}
