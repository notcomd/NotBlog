namespace Identity.Domain.Dto.Request;

/// <summary>
/// 验证码确认请求
/// </summary>
public record ConfirmEmailCodeRequest(string Email, string Code);
