using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.Command;

public class CreateNotFileGroupCommandHandler(
    INotFileGroupRepository notFileGroupRepository
    )
    : NotMediator.IRequestHandler<CreateNotFileGroupCommand, bool>
{
    public async Task<bool> Handler(CreateNotFileGroupCommand request, CancellationToken cancellationToken)
    {
        if (request.UserGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileGroupName))
            throw new ArgumentException("文件组名称不能为空");

        // 同级（根级）名称唯一性校验
        if (await notFileGroupRepository.ExistsByNameAtSameLevelAsync(null, request.FileGroupName))
            throw new InvalidOperationException($"文件组名称 '{request.FileGroupName}' 已存在");

        var data = new NotFileGroup.NotFileGroupBuilder()
            .WithUserId(request.UserGuid)
            .WithFileGroupName(request.FileGroupName)
            .WithFileGroupDescription(request.FileGroupDescription)
            .WithFileGroupTags(request.FileGroupTags)
            .WithFileIdentity(request.FileIdentity)
            .WithParentGroupId(request.ParentGroupId)
            .Build();

        await notFileGroupRepository.InsertNotFileGroupAsync(data);
        await notFileGroupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return true;
    }

    public class CreateNotFileGroupIdentifiedCommandHandler(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<CreateNotFileGroupIdentifiedCommandHandler> logger)
        : IdentifiedCommandHandler<CreateNotFileGroupCommand, bool>(mediator, requestManagement, logger)
    {
        protected override bool CreateResultForDuplicateRequest() => true;
    }
}
