using System.Text;
using System.Text.RegularExpressions;

namespace Message.Web.API.Services;

/// <summary>
/// 轻量 HTML 内容净化工具（S-17 XSS 防护）。
/// <para>
/// 本项目未引入 Ganss.XSS/HtmlSanitizer 等成熟清洗库，故提供此静态工具类：
/// 先通过正则移除 <c>&lt;script&gt;</c>、事件属性、危险协议等危险模式，再执行
/// HtmlEncode 风格转义（<c>&amp;&lt;&gt;"'</c>），双保险保证存储结果为无标签执行能力的纯文本。
/// </para>
/// </summary>
public static class SafeContentSanitizer
{
    /// <summary>危险模式正则：脚本块、危险标签、事件属性、危险协议（大小写不敏感）。</summary>
    private static readonly Regex DangerousPatternRegex = new(
        @"<script[\s\S]*?</script\s*>"
        + @"|<\s*(?:iframe|object|embed|link|meta|style|form|base|svg|math)\b[^>]*>"
        + @"|</\s*(?:script|iframe|object|embed|style)\b[^>]*>"
        + @"|javascript\s*:"
        + @"|vbscript\s*:"
        + @"|data\s*:\s*text/html"
        + @"|on[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 净化用户生成内容：去除危险模式 + HTML 实体转义。
    /// </summary>
    /// <param name="content">原始文本</param>
    /// <returns>净化后的纯文本（不会为 null）</returns>
    public static string Sanitize(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return content ?? string.Empty;

        var withoutDangerous = DangerousPatternRegex.Replace(content, string.Empty);
        return HtmlEncode(withoutDangerous);
    }

    /// <summary>
    /// HtmlEncode 风格转义：<c>&amp; &lt; &gt; &quot; &#39;</c>。
    /// <c>&amp;</c> 必须最先替换，避免二次转义。
    /// </summary>
    private static string HtmlEncode(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&#39;",
                _ => c.ToString()
            });
        }

        return builder.ToString();
    }
}
