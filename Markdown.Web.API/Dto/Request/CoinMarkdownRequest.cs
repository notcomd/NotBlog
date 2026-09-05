namespace Markdown.Web.API.Dto.Request;

/// <summary>
/// 文档打赏（硬币）请求
/// </summary>
public class CoinMarkdownRequest
{
    /// <summary>
    /// 打赏硬币数量（1~100）
    /// </summary>
    [Range(1, 100, ErrorMessage = "打赏数量必须在 1~100 之间")]
    public int Amount { get; set; }
}
