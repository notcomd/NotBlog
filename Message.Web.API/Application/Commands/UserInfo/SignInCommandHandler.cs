namespace Message.Web.API.Application.Commands.UserInfo;
using Microsoft.EntityFrameworkCore;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>
/// 每日签到命令处理程序（设计文档 4.2）：固定 +250 经验；当天重复签到抛业务异常（API 映射 400）；
/// (UserId, SignInDate) 唯一索引兜底并发重复签到。
/// </summary>
public class SignInCommandHandler(
    IUserSignInRepository signInRepository,
    IUserInfoRepository userInfoRepository,
    ILogger<SignInCommandHandler> logger) : IRequestHandler<SignInCommand, SignInResultDto>
{
    /// <summary>签到固定经验（设计文档 4.2）</summary>
    public const long SignInExperience = 250;

    public async Task<SignInResultDto> Handler(SignInCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 先查后插（TOCTOU 由唯一索引兜底）
        if (await signInRepository.IsSignedInAsync(command.UserId, today))
            throw new InvalidOperationException("今天已签到");

        var signIn = new UserSignIn(command.UserId, today);
        await signInRepository.AddAsync(signIn);

        // 经验发放（UserInfo upsert）
        var userInfo = await userInfoRepository.GetByUserIdAsync(command.UserId);
        if (userInfo is null)
        {
            userInfo = UserInfoEntity.Create(command.UserId);
            await userInfoRepository.AddAsync(userInfo);
        }
        var upgraded = userInfo.AddExperience(SignInExperience);
        await userInfoRepository.UpdateAsync(userInfo);

        try
        {
            await userInfoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // 并发重复签到：唯一索引 (UserId, SignInDate) 兜底
            logger.LogWarning("并发签到冲突已拦截：UserId={UserId}, Date={Date}", command.UserId, today);
            throw new InvalidOperationException("今天已签到");
        }

        logger.LogInformation("用户 {UserId} 签到成功 +{Exp} 经验，升级 {Upgraded} 级，当前等级 {Level}",
            command.UserId, SignInExperience, upgraded, userInfo.Level);

        return new SignInResultDto(userInfo.Level, userInfo.Experience, userInfo.Coins, upgraded);
    }
}
