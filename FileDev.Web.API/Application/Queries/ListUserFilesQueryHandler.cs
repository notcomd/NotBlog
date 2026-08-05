namespace FileDev.Web.API.Application.Queries;

/// <summary>列出用户文件（内存分页，与既有 gRPC/HTTP 行为一致）</summary>
public class ListUserFilesQueryHandler(
    INotFileService notFileService)
    : NotMediator.IRequestHandler<ListUserFilesQuery, UserFileListResult>
{
    public async Task<UserFileListResult> Handler(ListUserFilesQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        // GetFilesByUserIdAsync 已过滤已删除文件（!IsDeleted）
        var allFiles = await notFileService.GetFilesByUserIdAsync(request.UserId);
        var fileList = allFiles.ToList();
        var totalCount = fileList.Count;

        var pagedFiles = fileList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new UserFileListResult(pagedFiles, totalCount);
    }
}
