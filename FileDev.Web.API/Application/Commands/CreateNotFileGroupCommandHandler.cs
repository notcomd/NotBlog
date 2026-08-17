using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Commands;

public class CreateNotFileGroupCommandHandler(
    INotFileGroupRepository notFileGroupRepository
    )
    :  IRequestHandler<CreateNotFileGroupCommand, bool>
{
    public async Task<bool> Handler(CreateNotFileGroupCommand request, CancellationToken cancellationToken)
    {
        if (request.UserGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileGroupName))
            throw new ArgumentException("文件组名称不能为空");

        // 校验父文件组：父组必须存在且属于当前用户，否则拒绝创建（防止越权）
        NotFileGroup? parent = null;
        if (request.ParentGroupId.HasValue)
        {
            parent = await notFileGroupRepository.GetNotFileGroupByIdAsync(request.ParentGroupId.Value);
            // Critical：父组不存在时直接抛 NPE，需先判空再访问 UserId
            if (parent is null)
                throw new InvalidOperationException("指定的父文件组不存在");
            if (parent.UserId != request.UserGuid)
                throw new UnauthorizedAccessException("无权在他人文件组下创建子组");
        }

        // 同级名称唯一性校验（限定当前用户与父级层级）
        if (await notFileGroupRepository.ExistsByNameAtSameLevelAsync(request.UserGuid, request.ParentGroupId, request.FileGroupName))
            throw new InvalidOperationException($"文件组名称 '{request.FileGroupName}' 已存在");

        var data = new NotFileGroup.NotFileGroupBuilder()
            .WithUserId(request.UserGuid)
            .WithFileGroupName(request.FileGroupName)
            .WithFileGroupDescription(request.FileGroupDescription)
            .WithFileGroupTags(request.FileGroupTags)
            .WithFileIdentity(request.FileIdentity)
            .WithParent(parent)
            .Build();

        await notFileGroupRepository.InsertNotFileGroupAsync(data);
        await notFileGroupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return true;
    }
}
