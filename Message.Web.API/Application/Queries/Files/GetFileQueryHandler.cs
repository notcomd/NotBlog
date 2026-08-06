namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 获取文件信息查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可获取，无权访问返回 null。</para>
/// </summary>
public class GetFileQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetFileQuery, FileAttachment?>
{
    public async Task<FileAttachment?> Handler(GetFileQuery query, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(query.FileId);
        if (file == null)
            return null;

        var callerId = currentUser.GetUserId();
        return await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId)
            ? file
            : null;
    }
}
