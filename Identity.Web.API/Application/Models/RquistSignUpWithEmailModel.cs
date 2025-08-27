namespace Identity.Web.API.Application.Models;

public class RquistSignUpWithEmailModel
{
    public RquistSignUpWithEmailModel(string signUpEmail, string hashPassword, string generateCode)
    {
        SignUpEmail = signUpEmail;
        HashPassword = hashPassword;
        GenerateCode = generateCode;
    }

    public string SignUpEmail { get; set; }

    public string HashPassword { get; set; }

    public string GenerateCode { get; set; } 
}
