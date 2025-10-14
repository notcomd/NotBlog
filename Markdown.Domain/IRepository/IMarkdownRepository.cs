using DomainCommon;
using Markdown.Domain.Entities;

namespace Markdown.Domain.IRepository;

public interface IMarkdownRepository:IRepository<MarkDown>
{

    /// <summary>
    /// 上传MarkDown文件
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="markdownName">MarkDown文件名</param>
    /// <returns>MarkDown实体的GUID</returns>
    Task<string> MarkDownUploadAsync(Stream fileStream, string markdownName);

    /// <summary>
    /// 根据MarkDown实体的GUID获取MarkDown实体
    /// </summary>
    /// <param name="markDownGuid">MarkDown实体的GUID</param>
    /// <returns>MarkDown实体</returns>
    Task<MarkDown?> GetMarkDownAsync(Guid markDownGuid);

    /// <summary>
    /// 根据MarkDownGroup实体的GUID获取MarkDownGroup实体
    /// </summary>
    /// <param name="markDownGroupGuid">MarkDownGroup实体的GUID</param>
    /// <returns>MarkDownGroup实体</returns>
    Task<MarkDownGroup?> GetMarkDownGroupAsync(Guid markDownGroupGuid);
    
    
}