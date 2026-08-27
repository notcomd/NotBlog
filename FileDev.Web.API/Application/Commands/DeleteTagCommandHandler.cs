using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

/// <summary>删除标签命令处理器（软删除，文件本身不受影响）。</summary>
public class DeleteTagCommandHandler(
    INotFileTagRepository notFileTagRepository)
    : IRequestHandler<DeleteTagCommand, bool>
{
    public async Task<bool> Handler(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await notFileTagRepository.GetNotFileTagByIdAsync(request.TagId);
        if (tag.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作他人的标签");

        tag.SoftDelete();
        await notFileTagRepository.UpdateNotFileTagAsync(tag);
        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}