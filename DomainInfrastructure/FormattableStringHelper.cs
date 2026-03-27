namespace DomainInfrastructure;

/// <summary>
/// FormattableString 扩展方法
/// 用于安全构建 URI 字符串
/// </summary>
public static class FormattableStringHelper
{
    /// <summary>
    /// 将 FormattableString 转换为 URL 安全的字符串，自动对参数进行 Uri 编码
    /// </summary>
    public static string ToUriEncoded(this FormattableString formattableString)
    {
        var encodedArgs = formattableString.GetArguments()
            .Select(arg => (object)Uri.EscapeDataString(FormattableString.Invariant($"{arg}")))
            .ToArray();
        return string.Format(formattableString.Format, encodedArgs);
    }
}