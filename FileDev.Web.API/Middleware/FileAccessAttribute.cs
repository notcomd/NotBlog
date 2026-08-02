namespace FileDev.Web.API.Middleware;

/// <summary>文件ID来源</summary>
public enum FileIdSource
{
    /// <summary>从路由参数提取</summary>
    Route,
    /// <summary>从查询字符串提取</summary>
    Query
}

/// <summary>文件访问控制策略</summary>
public enum FileAccessPolicy
{
    /// <summary>仅文件所有者可访问</summary>
    OwnerOnly,
    /// <summary>已认证用户即可访问</summary>
    AuthenticatedOnly
}

/// <summary>
/// 标记需要进行文件访问权限验证的端点。
/// 中间件检测到该标记后，自动提取文件ID并执行访问控制策略。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class FileAccessAttribute : Attribute
{
    /// <summary>路由参数或查询字符串中的文件ID参数名（默认 "fileId"）</summary>
    public string FileIdParamName { get; init; } = "fileId";

    /// <summary>文件ID的来源（默认从路由参数提取）</summary>
    public FileIdSource Source { get; init; } = FileIdSource.Route;

    /// <summary>访问控制策略（默认仅所有者可访问）</summary>
    public FileAccessPolicy Policy { get; init; } = FileAccessPolicy.OwnerOnly;

    public FileAccessAttribute() { }

    /// <param name="fileIdParamName">文件ID参数名</param>
    public FileAccessAttribute(string fileIdParamName)
    {
        FileIdParamName = fileIdParamName;
    }
}
