namespace Notcomd.Token.JWT;

public class JwtOptions
{
    public string Issuer { get; set; }
    public string Audiencs { get; set; }
    public string PrivateKey { get; set; }
    public int ExpirSeconds { get; set; }
}