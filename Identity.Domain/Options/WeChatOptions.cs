namespace Identity.Domain.Options;

public class WeChatOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}