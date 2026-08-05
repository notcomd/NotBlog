namespace FileDev.Web.API.Application.Commands;
using FileDev.Domain.IRepository;

public class FileGroupAddFileCommandHandler(
    INotFileGroupRepository notFileGroupRepository,
    INotFileRepository notFileRepository)
    : NotMediator.IRequestHandler<FileGroupAddFileCommand, bool>
{
    public async Task<bool> Handler(FileGroupAddFileCommand command, CancellationToken cancellationToken)
    {
        var fileGroup = await notFileGroupRepository.GetNotFileGroupByIdAsync(command.FileGroupId);
        if (fileGroup is null)
            return false;

        // 越权校验：仅文件组属主可向组内添加文件（防止操作他人文件组）
        if (fileGroup.UserId != command.UserId)
            return false;

        // Critical 越权校验：待加入的文件必须属于当前用户，防止把他人文件加入自己的组
        var file = await notFileRepository.GetFileByIdAsync(command.FileId);
        if (file is null || file.IsDeleted)
            return false;
        if (file.UserId != command.UserId)
            return false;

        fileGroup.AddFile(command.FileId);
        await notFileGroupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}