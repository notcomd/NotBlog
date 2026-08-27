using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

/// <summary>创建标签命令处理器。用户内标签名唯一（唯一约束兜底并发冲突）。</summary>
public class CreateTagCommandHandler(
    INotFileTagRepository notFileTagRepository)
    : IRequestHandler<CreateTagCommand, bool>
{
    public async Task<bool> Handler(CreateTagCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(request));
        if (string.IsNullOrWhiteSpace(request.TagName))
            throw new ArgumentException("标签名称不能为空", nameof(request));

        var name = request.TagName.Trim();
        if (await notFileTagRepository.ExistsByUserIdAndNameAsync(request.UserId, name))
            throw new InvalidOperationException($"标签 '{name}' 已存在");

        var tag = new NotFileTag(request.UserId, name, request.TagDescription);
        await notFileTagRepository.InsertNotFileTagAsync(tag);
        await notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return true;
    }
}