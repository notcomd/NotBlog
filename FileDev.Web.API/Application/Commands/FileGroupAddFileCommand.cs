namespace FileDev.Web.API.Application.Commands;


public record FileGroupAddFileCommand(
    Guid FileGroupId,
    Guid FileId,
    Guid UserId
   ): IRequest<bool>;
    