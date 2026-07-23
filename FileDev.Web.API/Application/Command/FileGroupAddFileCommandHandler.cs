namespace FileDev.Web.API.Application.Command;
using FileDev.Domain.IRepository;

public class FileGroupAddFileCommandHandler(
    INotFileGroupRepository notFileGroupRepository)
    : NotMediator.IRequestHandler<FileGroupAddFileCommand, bool>
{
    public async Task<bool> Handler(FileGroupAddFileCommand command, CancellationToken cancellationToken)
    {
        var fileGroup = await notFileGroupRepository.GetNotFileGroupByIdAsync(command.FileGroupId);
        if (fileGroup is null)
            return false;

        fileGroup.AddFile(command.FileId);
        await notFileGroupRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        return true;
    }
}
