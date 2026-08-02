using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class ChangeByPasswordCommandHandler(IUserRepository userRepository)
    : NotMediator.IRequestHandler<ChangeByPasswordCommand, bool>
{
    public async Task<bool> Handler(ChangeByPasswordCommand request, CancellationToken cancellationToken)
    {
        // S-20：按认证用户 ID 定位，杜绝"仅凭 email 修改他人密码"
        var data = await userRepository.FindOneByUserAsync(request.UserId);

        if (data is null)
        {
            return false;
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