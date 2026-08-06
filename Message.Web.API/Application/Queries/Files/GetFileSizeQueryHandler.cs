namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 获取文件格式化大小查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可查看，无权访问返回 null。</para>
/// </summary>
public class GetFileSizeQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetFileSizeQuery, string?>
{
    public async Task<string?> Handler(GetFileSizeQuery query, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(query.FileId);
        if (file == null)
            return null;

        var callerId = currentUser.GetUserId();
        if (!await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId))
            return null;

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = file.FileSize;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
