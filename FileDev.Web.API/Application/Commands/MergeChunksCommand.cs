namespace FileDev.Web.API.Application.Commands;

using FileDev.Domain.Entities;
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
}
