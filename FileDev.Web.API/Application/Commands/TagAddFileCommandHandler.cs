using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

/// <summary>将文件加入标签的命令处理器。执行越权校验：标签与文件必须都属于当前用户。</summary>
public class TagAddFileCommandHandler(
    INotFileTagRepository notFileTagRepository,
    INotFileRepository notFileRepository)
    : IRequestHandler<TagAddFileCommand, bool>
{
    public async Task<bool> Handler(TagAddFileCommand request, CancellationToken cancellationToken)
    {
        var tag = await notFileTagRepository.GetNotFileTagByIdAsync(request.TagId);
        if (tag.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作他人的标签");

        // 待加入的文件必须属于当前用户，防止把他人文件加入自己的标签
        var file = await notFileRepository.GetFileByIdAsync(request.FileId);
        if (file is null || file.IsDeleted)
            return false;
        if (file.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权将他人文件加入标签");

        tag.AddFile(request.FileId);
        await notFileTagRepository.UpdateNotFileTagAsync(tag);
        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}