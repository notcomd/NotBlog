namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 检查文件是否存在查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：无权访问的文件视为不存在，避免泄露文件存在性。</para>
/// </summary>
public class FileExistsQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<FileExistsQuery, bool>
{
    public async Task<bool> Handler(FileExistsQuery query, CancellationToken cancellationToken)
    {
        if (!await fileRepository.ExistsAsync(query.FileId))
            return false;

        var callerId = currentUser.GetUserId();
        return await FileAccessGuard.CanAccessAsync(fileRepository, messageRepository, sessionRepository,
            query.FileId, callerId);
    }
}
