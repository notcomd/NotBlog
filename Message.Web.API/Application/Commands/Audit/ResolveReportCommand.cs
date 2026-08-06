namespace Message.Web.API.Application.Commands.Audit;
/// <summary>
/// 处理举报命令。
/// <para>CQRS 命令侧：仅返回操作结果（bool），不返回业务实体/DTO。</para>
/// </summary>
/// <param name="ReportGuid">举报 ID</param>
/// <param name="ReviewerGuid">审核人用户 ID</param>
/// <param name="Note">审核备注</param>
/// <param name="IsContentRemoved">是否删除被举报内容</param>
public record ResolveReportCommand(Guid ReportGuid, Guid ReviewerGuid, string? Note, bool IsContentRemoved) : IRequest<bool>;

