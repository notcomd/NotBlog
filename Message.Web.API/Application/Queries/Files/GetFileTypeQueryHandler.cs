namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 获取文件类型分类信息查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可查看，无权访问返回 File 为 null 的结果。</para>
/// </summary>
public class GetFileTypeQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetFileTypeQuery, FileTypeQueryResult>
{
    public async Task<FileTypeQueryResult> Handler(GetFileTypeQuery query, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(query.FileId);
        if (file == null)
            return new FileTypeQueryResult(null!, false, false, false, false);

        var callerId = currentUser.GetUserId();
        if (!await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId))
            return new FileTypeQueryResult(null!, false, false, false, false);

        return new FileTypeQueryResult(
            file,
            file.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
            file.FileType.StartsWith("video/", StringComparison.OrdinalIgnoreCase),
            file.FileType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase),
            file.FileType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase)
                || file.FileType.StartsWith("application/msword", StringComparison.OrdinalIgnoreCase)
                || file.FileType.StartsWith("application/vnd.openxmlformats-officedocument", StringComparison.OrdinalIgnoreCase)
                || file.FileType.StartsWith("text/", StringComparison.OrdinalIgnoreCase));
    }
}
