
namespace Message.Web.API.Application;

/// <summary>
/// 文件访问授权守卫（修复 S-05）。
/// <para>
/// 文件附件属于消息聚合（FileAttachment → Message），本身不记录上传者；
/// 访问授权规则：调用者是文件所属消息的<b>发送者（上传者）</b>，
/// 或是消息所在会话的<b>参与者</b>，二者满足其一即可访问；否则视为无权访问。
/// </para>
/// </summary>
public static class FileAccessGuard
{
    /// <summary>
    /// 校验调用者是否有权访问指定附件（按附件 ID 加载后校验）。
    /// </summary>
    public static async Task<bool> CanAccessAsync(
        IFileAttachmentRepository fileRepository,
        IMessageRepository messageRepository,
        IChatSessionRepository sessionRepository,
        Guid fileId,
        Guid callerId)
    {
        var file = await fileRepository.GetByIdAsync(fileId);
        if (file == null)
            return false;

        return await CanAccessAsync(file, messageRepository, sessionRepository, callerId);
    }

    /// <summary>
    /// 校验调用者是否有权访问已加载的附件。
    /// </summary>
    public static async Task<bool> CanAccessAsync(
        FileAttachment file,
        IMessageRepository messageRepository,
        IChatSessionRepository sessionRepository,
        Guid callerId)
    {
        var message = await messageRepository.GetByIdAsync(file.MessageId);
        if (message == null)
            return false;

        // 上传者本人始终可访问
        if (message.SenderId == callerId)
            return true;

        // 所属消息所在会话的参与者可访问
        var session = await sessionRepository.GetByIdAsync(message.SessionId);
        return session != null && session.IsParticipant(callerId);
    }
}
