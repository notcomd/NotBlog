using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

/// <summary>重命名标签命令处理器，用户内同名唯一校验（排除自身）。</summary>
public class RenameTagCommandHandler(
    INotFileTagRepository notFileTagRepository)
    : IRequestHandler<RenameTagCommand, bool>
{
    public async Task<bool> Handler(RenameTagCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
            throw new ArgumentException("标签名称不能为空", nameof(request));

        var tag = await notFileTagRepository.GetNotFileTagByIdAsync(request.TagId);
        if (tag.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权操作他人的标签");

        var newName = request.NewName.Trim();
        if (await notFileTagRepository.ExistsByUserIdAndNameAsync(request.UserId, newName, request.TagId))
            throw new InvalidOperationException($"标签 '{newName}' 已存在");

        tag.Rename(newName);
        await notFileTagRepository.UpdateNotFileTagAsync(tag);
        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}