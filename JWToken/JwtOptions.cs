namespace Notcomd.Token.JWT;

public class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audiencs { get; set; }=null!;
    public string PrivateKey { get; set; }=null!;
    public int ExpirSeconds { get; set; }
}