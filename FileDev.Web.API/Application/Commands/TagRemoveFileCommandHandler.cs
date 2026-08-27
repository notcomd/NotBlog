using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

/// <summary>从标签移除文件的命令处理器。</summary>
public class TagRemoveFileCommandHandler(
    INotFileTagRepository notFileTagRepository)
    : IRequestHandler<TagRemoveFileCommand, bool>
{
    public async Task<bool> Handler(TagRemoveFileCommand request, CancellationToken cancellationToken)
    {
        var tag = await notFileTagRepository.GetNotFileTagByIdAsync(request.TagId);
        if (tag.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作他人的标签");

        tag.RemoveFile(request.FileId);
        await notFileTagRepository.UpdateNotFileTagAsync(tag);
        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}