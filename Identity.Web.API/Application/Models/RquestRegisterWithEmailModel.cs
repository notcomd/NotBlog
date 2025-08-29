namespace Identity.Web.API.Application.Models;

public class RquestRegisterWithEmailModel
{
    public RquestRegisterWithEmailModel(string registerEmail, string hashPassword, string generateCode)
    {
        RegisterEmail = registerEmail;
        HashPassword = hashPassword;
        GenerateCode = generateCode;
    }

    public string RegisterEmail { get; set; }

    public string HashPassword { get; set; }

    public string GenerateCode { get; set; } 
}
