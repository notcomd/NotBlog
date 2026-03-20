namespace Identity.Domain.Options;

public class MicrosoftOptions
{
    
    
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    
    public bool Enable { get; set; } = true;
}