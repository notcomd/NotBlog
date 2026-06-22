namespace Identity.Domain.Dto.Request;

public record LoginRequest(
    string Email,
    string Password,
    string Code,
    string Provider,
    string RedirectUri,
    string ClientId,
    string ClientSecret,
    string GrantType)
{
    /// <summary>
    /// 电子邮件地址
    /// </summary>
    [EmailAddress(ErrorMessage = "无效邮件地址")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 验证码（当前未使用）
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// OAuth提供程序
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 重定向 URI
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 客户端 ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}