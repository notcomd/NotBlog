namespace Identity.Web.API.Application.Command;

public class ChangeByUserCommand : IRequest<bool>
{
    public ChangeByUserCommand(string email, string userName, string address, Uri? imageCoverUri)
    {
        Email = email;
        UserName = userName;
        Address = address;
        ImageCoverUri = imageCoverUri;       
    }

    public string Email { get; init; }         

    public string UserName { get; set; }     

    public string Address { get; set; }

    public Uri? ImageCoverUri { get; set; }


}
