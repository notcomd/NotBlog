namespace Identity.Web.API.Application.Commands;

public class CreateMenuCommandHandler(
    IMenuRepository menuRepository,
    ILogger<CreateMenuCommandHandler> logger)
    : IRequestHandler<CreateMenuCommand, CreateMenuResult>
{
    public async Task<CreateMenuResult> Handler(
        CreateMenuCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.MenuName))
            throw new InvalidOperationException("菜单名称不能为空");

        // 挂载到指定父节点时，父节点必须存在且未删除
        if (command.ParentId is { } parentId)
        {
            var parent = await menuRepository.FindByIdAsync(parentId, ct)
                ?? throw new InvalidOperationException($"父菜单 '{parentId}' 不存在");
            if (parent.IsDeleted)
                throw new InvalidOperationException("父菜单已删除，无法在其下创建");
        }

        // 路由路径唯一（空表示目录，不校验）
        var url = command.Url?.Trim();
        if (!string.IsNullOrEmpty(url) && await menuRepository.UrlExistsAsync(url, null, ct))
            throw new InvalidOperationException($"菜单路径 '{url}' 已存在");

        var menu = new Menu(
            command.MenuName,
            command.MenuType,
            command.ParentId,
            command.Url,
            command.Icon,
            command.SortOrder,
            command.RequiredPermissionCode,
            command.RequiredRole);

        await menuRepository.AddAsync(menu, ct);
        await menuRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[CreateMenu] 创建成功: Name={Name}, Id={Id}, Type={Type}, ParentId={ParentId}",
            menu.MenuName, menu.MenuId, menu.MenuType, menu.ParentId);

        return new CreateMenuResult(menu.MenuId, menu.MenuName);
    }
}

public class UpdateMenuCommandHandler(
    IMenuRepository menuRepository,
    ILogger<UpdateMenuCommandHandler> logger)
    : IRequestHandler<UpdateMenuCommand, bool>
{
    public async Task<bool> Handler(
        UpdateMenuCommand command, CancellationToken ct)
    {
        var menu = await menuRepository.FindByIdAsync(command.MenuId, ct)
            ?? throw new InvalidOperationException($"菜单 '{command.MenuId}' 不存在");

        if (menu.IsDeleted)
            throw new InvalidOperationException("无法更新已删除的菜单");

        // 路由路径唯一（仅当显式变更且非清空时校验，排除自身）
        if (command.Url is { Length: > 0 } newUrl
            && await menuRepository.UrlExistsAsync(newUrl.Trim(), command.MenuId, ct))
            throw new InvalidOperationException($"菜单路径 '{newUrl}' 已存在");

        var name = command.MenuName ?? menu.MenuName;
        var type = command.MenuType ?? menu.MenuType;
        menu.ChangeMenu(name, type, command.Url, command.Icon, command.SortOrder,
            command.RequiredPermissionCode, command.RequiredRole);

        if (command.IsEnabled is { } enabled)
        {
            if (enabled) menu.Enable();
            else menu.Disable();
        }

        // 父节点变更：null=不改，Guid.Empty=移至根，其他=挂到新父下（防成环）
        if (command.ParentId is { } newParent)
        {
            if (newParent == Guid.Empty)
            {
                menu.ChangeParent(null);
            }
            else
            {
                await EnsureNoCycleAsync(menuRepository, menu.MenuId, newParent, ct);
                menu.ChangeParent(newParent);
            }
        }

        await menuRepository.UpdateAsync(menu, ct);
        await menuRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[UpdateMenu] 更新成功: Id={Id}", command.MenuId);
        return true;
    }

    /// <summary>
    /// 防成环：沿新父节点向上追溯，若链上遇到目标节点自身则拒绝
    /// （把节点挂到自己的子孙节点下会形成循环树）。
    /// </summary>
    private static async Task EnsureNoCycleAsync(
        IMenuRepository repo, Guid menuId, Guid newParentId, CancellationToken ct)
    {
        var cursor = newParentId;
        var hops = 0;
        while (cursor != Guid.Empty && cursor != menuId && hops++ < 200)
        {
            var parent = await repo.FindByIdAsync(cursor, ct);
            if (parent is null)
                throw new InvalidOperationException($"父菜单 '{cursor}' 不存在");
            cursor = parent.ParentId ?? Guid.Empty;
        }

        if (cursor == menuId)
            throw new InvalidOperationException("不能将菜单移动到自己的子孙节点下（会形成循环）");
    }
}

public class DeleteMenuCommandHandler(
    IMenuRepository menuRepository,
    ILogger<DeleteMenuCommandHandler> logger)
    : IRequestHandler<DeleteMenuCommand, bool>
{
    public async Task<bool> Handler(
        DeleteMenuCommand command, CancellationToken ct)
    {
        var menu = await menuRepository.FindByIdAsync(command.MenuId, ct)
            ?? throw new InvalidOperationException($"菜单 '{command.MenuId}' 不存在");

        // 树语义：存在未删除子节点时禁止删除（先处理子节点，防止树悬挂）
        var children = await menuRepository.GetChildrenAsync(command.MenuId, ct);
        if (children.Any(c => !c.IsDeleted))
            throw new InvalidOperationException("该菜单下存在子菜单，请先删除或移动子菜单");

        var deleted = await menuRepository.DeleteAsync(command.MenuId, ct);
        if (!deleted)
            throw new InvalidOperationException($"菜单 '{command.MenuId}' 不存在");

        await menuRepository.UnitOfWork.SaveEntitiesAsync(ct);

        logger.LogInformation("[DeleteMenu] 软删除成功: Id={Id}", command.MenuId);
        return true;
    }
}