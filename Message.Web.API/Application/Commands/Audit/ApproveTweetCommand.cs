namespace Message.Web.API.Application.Commands.Audit;
/// <summary>
/// 通过推文审核命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="TweetGuid">推文 ID</param>
/// <param name="AuditorGuid">审核人用户 ID</param>
public record ApproveTweetCommand(Guid TweetGuid, Guid AuditorGuid) : IRequest<bool>;

