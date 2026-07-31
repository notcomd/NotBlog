using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class UploadAvatarIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<UploadAvatarCommand, UploadAvatarResult>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<UploadAvatarCommand, UploadAvatarResult>(logger, mediator, requestManagement)
{
    protected override UploadAvatarResult CreateResultForDuplicateRequest()
    {
        return new UploadAvatarResult(string.Empty, string.Empty, string.Empty, 0, 0, 0, string.Empty);
    }
}
