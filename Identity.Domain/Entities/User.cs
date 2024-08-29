using System.ComponentModel.DataAnnotations;
using Markdown.Domain;

namespace Identity.Domain.Entities;

public class User:IAggregateRoot
{
    
    public Guid UserGuid { get; init; }
    public Guid UserRoleGuid { get; init; }
    public string UserName { get; private set; }
    public Uri HeadImage { get; private set; }
    public string? UserEmail { get; private set; }
    public PhoneNumber? UserPhone { get; private set; }
    public string? UserAddress { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    
    public DateTime CreateDatetime { get; init; }
    
    
    private User(){}

    public User(ref string userEmail, PhoneNumber phoneNumber,string userName,Guid userRoleGuid)
    {
        UserGuid = new Guid();
        UserName = userName;
        UserEmail = userEmail;
        UserPhone = phoneNumber;
        UserRoleGuid = userRoleGuid;
    }

    public ValueTask<User> ChangeByAddressAsync(ref string userAddress)
    {
        this.UserAddress = userAddress;
        return new ValueTask<User>(this);
    } 
    
    public ValueTask ChangeByPasswordAsync(ref string hashPassword)
    {
        if (hashPassword.Length <= 8)
        {
            throw new ArgumentOutOfRangeException($"your are set password is short!");
        }
        else
        {
            PasswordHash = hashPassword;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask ChangeByEmailAsync([EmailAddress(ErrorMessage = "your set email is error ,pleas set again your email address!")]ref string newEmail)
    {
        UserEmail = newEmail;
        return ValueTask.CompletedTask;
    }

    public ValueTask ChangeByPhoneAsync(PhoneNumber phoneNumber)
    {
        UserPhone = phoneNumber;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> CheckByPasswordAsync(ref string hashPassword)
    {
        return new ValueTask<bool>(PasswordHash == HashH256Tool.CreateHash256Async(hashPassword).Result);
    }

    public ValueTask ChangeByHeadImageAsync(ref Uri imageUri)
    {
        HeadImage = imageUri;
        return ValueTask.CompletedTask;
    }
}