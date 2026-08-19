
namespace NotBlog_Yarp.Options;

public sealed class DevUser
{
    /// <summary>该用户拥有的所有权限编码</summary>
    public HashSet<string> Permissions { get; init; } = new();

    /// <summary>数据范围（格式: "type|value1,value2,..."）</summary>
    public Dictionary<string, HashSet<string>> DataScope { get; init; } = new();
}