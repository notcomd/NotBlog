namespace Message.Web.API.Hubs;

public interface IMessageClient
{
    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="message">消息内容</param>
    Task ReceiveMessage(MessageDto message);
    
    /// <summary>
    /// 消息被撤回
    /// </summary>
    /// <param name="messageId">消息ID</param>
    Task MessageRecalled(Guid messageId);

    /// <summary>
    /// 消息被读取
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="readerId">读取用户ID</param>
    Task MessageRead(Guid messageId, Guid readerId);

    /// <summary>
    /// 用户上线
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task UserOnline(Guid userId);

    /// <summary>
    /// 用户下线
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task UserOffline(Guid userId);

    /// <summary>
    /// 用户正在输入
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID</param>
    Task TypingIndicator(Guid sessionId, Guid userId);

    /// <summary>
    /// 未读消息数量更新
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="count">未读消息数量</param>
    Task UnreadCountUpdated(Guid sessionId, int count);

    /// <summary>
    /// 文件分片上传进度
    /// </summary>
    /// <param name="progress">上传进度信息（文件Key、已完成分片数、总分片数、百分比等）</param>
    Task UploadProgress(ChunkUploadProgress progress);
}