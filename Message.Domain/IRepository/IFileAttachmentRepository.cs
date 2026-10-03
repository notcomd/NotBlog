
namespace Message.Domain.IRepository;
/// <summary>
/// 文件附件仓储接口。
/// <para>
/// DDD 说明：<see cref="FileAttachment"/> 是 <see cref="Message"/> 聚合内的实体，
/// 依据"聚合内实体不独立持有仓储"原则，本接口<b>不</b>继承 <see cref="IRepository{T}"/>
/// （其泛型约束要求 T 为聚合根），仅提供查询投影与持久化辅助能力。
/// </para>
/// </summary>
public interface IFileAttachmentRepository
{
    /// <summary>按附件 ID 查询（不存在返回 null）</summary>
    Task<FileAttachment?> GetByIdAsync(Guid attachmentId);
    /// <summary>获取某消息下的全部附件</summary>
    Task<IEnumerable<FileAttachment>> GetByMessageIdAsync(Guid messageId);
    /// <summary>按文件类型查询附件</summary>
    Task<IEnumerable<FileAttachment>> GetByFileTypeAsync(string fileType);
    /// <summary>新增附件</summary>
    Task<FileAttachment> AddAsync(FileAttachment attachment);
    /// <summary>更新附件</summary>
    Task<FileAttachment> UpdateAsync(FileAttachment attachment);
    /// <summary>删除附件</summary>
    Task DeleteAsync(Guid attachmentId);
    /// <summary>判断附件是否存在</summary>
    Task<bool> ExistsAsync(Guid attachmentId);
    /// <summary>统计某消息的附件数量</summary>
    Task<int> CountByMessageAsync(Guid messageId);
}