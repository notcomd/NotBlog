namespace Notcomd.NotEmail.Core;

/// <summary>
/// 邮件接收接口（IMAP）
/// </summary>
public interface IEmailReceiver
{
    /// <summary>
    /// 获取收件箱邮件列表
    /// </summary>
    /// <param name="limit">获取数量，0 表示全部</param>
    Task<IReadOnlyList<ReceivedEmail>> GetInboxAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 UID 获取单封邮件
    /// </summary>
    Task<ReceivedEmail?> GetByUidAsync(uint uid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主题关键词搜索邮件
    /// </summary>
    Task<IReadOnlyList<ReceivedEmail>> SearchAsync(string keyword, int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定 UID 的邮件（软删除：标记为已删除）
    /// </summary>
    Task<bool> DeleteAsync(uint uid, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除邮件
    /// </summary>
    Task<int> DeleteBatchAsync(IEnumerable<uint> uids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 永久清除已标记删除的邮件
    /// </summary>
    Task ExpungeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记邮件为已读
    /// </summary>
    Task MarkAsReadAsync(uint uid, CancellationToken cancellationToken = default);
}