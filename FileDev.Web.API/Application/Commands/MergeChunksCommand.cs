namespace FileDev.Web.API.Application.Commands;

using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Domain.IServices;

public class MergeChunksCommand : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public string FileKey { get; set; } = null!;

    /// <summary>合并后的文件名（为空时回退到分片记录中的文件名）</summary>
    public string? FileName { get; set; }

    /// <summary>合并后的文件标签（为空时回退到分片记录中的标签）</summary>
    public HashSet<string>? FileTags { get; set; }

    /// <summary>合并后的文件描述（为空时回退到分片记录中的描述）</summary>
    public string? FileDescription { get; set; }

    /// <summary>来源：内容附件时所属的业务内容标识（如帖ID/MarkdownID/视频ID），为空则为用户文件仓库</summary>
    public string? ContentId { get; set; }

    /// <summary>来源：内容附件时的业务类型（与 ContentId 配套）</summary>
    public ContentType ContentType { get; set; }
}
