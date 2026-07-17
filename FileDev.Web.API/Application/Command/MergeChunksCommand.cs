namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.Entities;
using FileDev.Domain.IServices;

public class MergeChunksCommand : IRequest<NotFile>
{
    public Guid UserId { get; set; }
    public string FileKey { get; set; } = null!;
}
