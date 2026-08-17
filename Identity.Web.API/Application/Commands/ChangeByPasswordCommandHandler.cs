using Identity.Domain.ICache;
using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class ChangeByPasswordCommandHandler(
    IUserRepository userRepository,
    IIdentityCacheService identityCacheService)
    :  IRequestHandler<ChangeByPasswordCommand, bool>
{
    public async Task<bool> Handler(ChangeByPasswordCommand request, CancellationToken cancellationToken)
    {
        // S-20：按认证用户 ID 定位，杜绝"仅凭 email 修改他人密码"
        var data = await userRepository.FindOneByUserAsync(request.UserId);

        if (data is null)
        {
            return false;
        }

        // 邮箱验证码路径：必须校验验证码匹配后才允许改密（修复 hasCode 路径绕过校验漏洞）
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var cached = await identityCacheService.GetStringAsync($"Login_{data.UserEmail}", cancellationToken);
            if (string.IsNullOrEmpty(cached) || !string.Equals(cached, request.Code, StringComparison.Ordinal))
                return false;
            // 验证通过后消费验证码（一次性）
            await identityCacheService.RemoveAsync($"Login_{data.UserEmail}", cancellationToken);
        }

        await data.ChangeByPasswordAsync(request.NewPassword);
        await userRepository.UpdateByUserAsync(data);
        await userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class ChangeByPasswordIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<ChangeByPasswordCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<ChangeByPasswordCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}