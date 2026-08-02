namespace Message.Web.API.Services;

/// <summary>
/// 内置默认敏感词过滤器（S-17 内容安全）。
/// <para>
/// 采用"拒绝"策略：命中任一内置敏感词即判定为包含敏感内容，
/// 由调用方（Tweet 发布 / 消息发送命令）拒绝请求并提示用户。
/// </para>
/// </summary>
public static class SensitiveWordFilter
{
    /// <summary>内置默认敏感词列表（至少 10 个常见词，可后续扩展为配置/词典加载）。</summary>
    private static readonly string[] DefaultSensitiveWords =
    [
        "赌博", "诈骗", "色情", "裸聊", "博彩", "代开发票", "刷单", "毒品", "枪支", "假证",
        "卖淫", "嫖娼", "传销", "洗钱", "高利贷"
    ];

    /// <summary>
    /// 检测文本是否包含敏感词。
    /// </summary>
    /// <param name="content">待检测文本</param>
    /// <returns>
    /// <c>(true, 命中的敏感词)</c> 表示命中；<c>(false, string.Empty)</c> 表示未命中。
    /// </returns>
    public static (bool IsSensitive, string MatchedWord) ContainsSensitive(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return (false, string.Empty);

        foreach (var word in DefaultSensitiveWords)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
                return (true, word);
        }

        return (false, string.Empty);
    }
}
