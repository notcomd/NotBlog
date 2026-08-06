namespace Message.Web.API.Application.Queries.Files;
/// <summary>
/// 获取消息的全部附件查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// <para>权限（修复 S-05）：仅消息所属会话的参与者可查看附件。</para>
/// </summary>
public class GetMessageFilesQueryHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetMessageFilesQuery, IEnumerable<FileAttachment>>
{
    public async Task<IEnumerable<FileAttachment>> Handler(GetMessageFilesQuery query,
        CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(query.MessageId);
        if (message == null)
            return [];

        var session = await sessionRepository.GetByIdAsync(message.SessionId);
        var callerId = currentUser.GetUserId();
        if (session == null || !session.IsParticipant(callerId))
            return [];

        return await fileRepository.GetByMessageIdAsync(query.MessageId);
    }
}
