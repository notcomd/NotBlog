namespace FileDev.Web.API.Application.Command;


public record FileGroupAddFileCommand(
    Guid FileGroupId,
    Guid FileId,
    Guid UserId
   ): IRequest<bool>;
    