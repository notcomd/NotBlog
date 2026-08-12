
namespace Message.Web.API.Application.Commands.Community;
/// <summary>生成圈子邀请命令处理程序。</summary>
public class GenerateCircleInvitationCommandHandler(
    ICircleRepository circleRepository,
    ICircleInvitationRepository invitationRepository,
    ILogger<GenerateCircleInvitationCommandHandler> logger) : IRequestHandler<GenerateCircleInvitationCommand, CircleInvitationResult>
{
    // 邀请码字符集：去除易混淆字符 0/O/1/I
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxCodeAttempts = 10;

    // 邀请码周额度（滚动 7 天窗口）：圈主不限制，管理员每周 2 个，普通用户每周 1 个
    private static readonly TimeSpan QuotaWindow = TimeSpan.FromDays(7);
    private const int AdminWeeklyCodeQuota = 2;
    private const int MemberWeeklyCodeQuota = 1;

    public async Task<CircleInvitationResult> Handler(GenerateCircleInvitationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var circle = await circleRepository.GetByIdAsync(command.CircleGuid)
                ?? throw new KeyNotFoundException("圈子不存在");

            var operatorMember = await circleRepository.GetMemberAsync(command.CircleGuid, command.OperatorGuid);
            if (operatorMember is null || operatorMember.Status != CircleMemberStatus.Active)
                throw new UnauthorizedAccessException("只有圈子有效成员可以生成邀请");

            var ttl = command.TtlHours.HasValue ? TimeSpan.FromHours(command.TtlHours.Value) : CircleInvitation.DefaultTtl;
            if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromDays(30))
                throw new ArgumentException("邀请有效期必须在1小时到30天之间", nameof(command.TtlHours));

            // 邀请额度与权限：
            // - 邀请码：圈主不限制；管理员每周 2 个；普通用户每周 1 个（滚动 7 天窗口，按创建者统计）
            // - 链接邀请 / 直邀：仅圈主可创建，次数不限
            var type = command.Type.ToLowerInvariant();
            if (type is "link" or "direct")
            {
                if (operatorMember.UserGuid != circle.OwnerGuid)
                    throw new UnauthorizedAccessException("仅圈主可创建链接邀请或直邀");
            }
            else if (type == "code" && operatorMember.Role != CircleMemberRole.Owner)
            {
                var quota = operatorMember.Role == CircleMemberRole.Admin
                    ? AdminWeeklyCodeQuota
                    : MemberWeeklyCodeQuota;
                var createdInWindow = await invitationRepository.CountCodesCreatedSinceAsync(
                    command.OperatorGuid, DateTimeOffset.UtcNow - QuotaWindow);
                if (createdInWindow >= quota)
                    throw new InvalidOperationException(
                        $"本周邀请码额度已用完（管理员每周{AdminWeeklyCodeQuota}个/普通用户每周{MemberWeeklyCodeQuota}个）");
            }

            CircleInvitation invitation;
            switch (type)
            {
                case "code":
                    invitation = CircleInvitation.CreateCode(command.CircleGuid, command.OperatorGuid,
                        await GenerateUniqueCodeAsync(cancellationToken), ttl);
                    break;
                case "link":
                    invitation = CircleInvitation.CreateLink(command.CircleGuid, command.OperatorGuid, ttl);
                    break;
                case "direct":
                    invitation = CircleInvitation.CreateDirect(command.CircleGuid, command.OperatorGuid,
                        command.InviteeGuid ?? throw new ArgumentException("直邀必须指定被邀请用户", nameof(command.InviteeGuid)), ttl);
                    break;
                default:
                    throw new ArgumentException("邀请类型必须是 code / link / direct", nameof(command.Type));
            }

            await invitationRepository.AddAsync(invitation);
            await invitationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogInformation("邀请生成成功: Invite={InviteGuid}, Circle={CircleGuid}, Type={Type}",
                invitation.InviteGuid, command.CircleGuid, invitation.Type);
            return new CircleInvitationResult(invitation.InviteGuid, invitation.Code, invitation.Token);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException and not ArgumentException)
        {
            logger.LogError(ex, "生成邀请失败: Circle={CircleGuid}", command.CircleGuid);
            throw;
        }
    }

    /// <summary>生成全局唯一的 6 位邀请码（查重重试）</summary>
    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = new string(Enumerable.Range(0, CodeLength)
                .Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)])
                .ToArray());
            if (!await invitationRepository.CodeExistsAsync(code))
                return code;
        }
        throw new InvalidOperationException("邀请码生成失败，请重试");
    }
}
