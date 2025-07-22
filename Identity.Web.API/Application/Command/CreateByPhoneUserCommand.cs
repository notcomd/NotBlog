namespace Identity.Web.API.Application.Command;

public sealed record CreateByPhoneUserCommand(PhoneNumber PhoneNumber,string Password,string RoleName,string Attribute):IRequest<bool>;

