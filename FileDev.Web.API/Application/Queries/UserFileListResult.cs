namespace FileDev.Web.API.Application.Queries;

/// <summary>用户文件分页列表结果</summary>
public record UserFileListResult(IReadOnlyList<NotFile> Files, int TotalCount);
