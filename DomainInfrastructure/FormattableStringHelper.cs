namespace DomainInfrastructure;

/// <summary>
/// FormattableString 扩展方法
/// 用于安全构建 URL/URI 字符串，自动对插值参数进行 Uri 编码，防止注入攻击。
/// </summary>
public static class FormattableStringHelper
{
    /// <summary>
    /// 将 FormattableString 转换为 URL 安全的字符串，自动对参数进行 Uri 编码。
    /// 每个插值参数都会被 Uri.EscapeDataString 编码后重新插入格式字符串。
    /// </summary>
    /// <param name="formattableString">包含未编码参数的格式化字符串</param>
    /// <returns>所有参数已进行 Uri 编码的最终字符串</returns>
    /// <example>
    /// <code>
    /// var url = $"https://api.example.com/search?q={"hello world"}&lang={"zh-CN"}".ToUriEncoded();
    /// // 结果: https://api.example.com/search?q=hello%20world&lang=zh-CN
    /// </code>
    /// </example>
    public static string ToUriEncoded(this FormattableString formattableString)
    {
        var encodedArgs = formattableString.GetArguments()
            .Select(arg => (object)Uri.EscapeDataString(FormattableString.Invariant($"{arg}")))
            .ToArray();
        return string.Format(formattableString.Format, encodedArgs);
    }
}