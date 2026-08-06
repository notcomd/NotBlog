namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 获取文件下载次数查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可查看，无权访问返回 0。</para>
/// </summary>
public class GetDownloadCountQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetDownloadCountQuery, int>
{
    public async Task<int> Handler(GetDownloadCountQuery query, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(query.FileId);
        if (file == null)
            return 0;

        var callerId = currentUser.GetUserId();
        return await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId)
            ? file.DownloadCount
            : 0;
    }
}
