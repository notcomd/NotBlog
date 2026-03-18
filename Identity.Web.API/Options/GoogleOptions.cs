namespace Identity.Web.API.Options;

public class GoogleOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}