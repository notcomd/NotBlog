namespace FileDev.Web.API.Application.Queries;

/// <summary>列出用户文件（分页，S-08：仅允许列出调用者自己的文件）</summary>
public class ListUserFilesQuery : IRequest<UserFileListResult>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
