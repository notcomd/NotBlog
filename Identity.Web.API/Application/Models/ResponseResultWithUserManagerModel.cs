namespace Identity.Web.API.Application.Models;

public class ResponseResultWithUserManagerModel
{
    public ResponseResultWithUserManagerModel(string userEmail, string userName,  Uri? imageCover, string? address,
        WithResultPhone? withResultPhone,
        RequestChangeWithRoleModel? requestChangeWithRoleModel)
    {
        UserName = userName;
        UserEmail = userEmail;
        ImageCover = imageCover;
        Address = address;
        WithResultPhone = withResultPhone;
        RequestChangeWithRoleModel = requestChangeWithRoleModel;
    }

    public string UserName { get; }

    public string UserEmail { get; }

    public Uri? ImageCover { get; }

    public string? Address { get; }

    public WithResultPhone? WithResultPhone { get; }

    public RequestChangeWithRoleModel? RequestChangeWithRoleModel { get; }

}
