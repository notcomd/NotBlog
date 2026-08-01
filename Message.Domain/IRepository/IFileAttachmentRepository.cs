using Message.Domain.Entities;
using Message.Domain.SeedWork;

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
    Task<FileAttachment?> GetByIdAsync(Guid attachmentId);
    Task<IEnumerable<FileAttachment>> GetByMessageIdAsync(Guid messageId);
    Task<IEnumerable<FileAttachment>> GetByFileTypeAsync(string fileType);
    Task<FileAttachment> AddAsync(FileAttachment attachment);
    Task<FileAttachment> UpdateAsync(FileAttachment attachment);
    Task DeleteAsync(Guid attachmentId);
    Task<bool> ExistsAsync(Guid attachmentId);
    Task<int> CountByMessageAsync(Guid messageId);
}