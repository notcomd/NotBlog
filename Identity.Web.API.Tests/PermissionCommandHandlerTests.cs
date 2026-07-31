using Identity.Domain.Entities.RoleAggregate;
using Identity.Web.API.Application.Commands;
using Moq;
using System.Linq.Expressions;

namespace Identity.Web.API.Tests;

public class PermissionCommandHandlerTests
{
    private readonly Mock<IPermissionRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ILogger<CreatePermissionCommandHandler>> _createLog;
    private readonly Mock<ILogger<UpdatePermissionCommandHandler>> _updateLog;
    private readonly Mock<ILogger<DeletePermissionCommandHandler>> _deleteLog;

    public PermissionCommandHandlerTests()
    {
        _repo = new Mock<IPermissionRepository>();
        _uow = new Mock<IUnitOfWork>();
        _repo.Setup(r => r.UnitOfWork).Returns(_uow.Object);
        _createLog = new Mock<ILogger<CreatePermissionCommandHandler>>();
        _updateLog = new Mock<ILogger<UpdatePermissionCommandHandler>>();
        _deleteLog = new Mock<ILogger<DeletePermissionCommandHandler>>();
    }

    [Fact]
    public async Task CreatePermission_DuplicateCode_ShouldThrow()
    {
        _repo.Setup(r => r.CodeExistsAsync("ADMIN:READ", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreatePermissionCommandHandler(_repo.Object, _createLog.Object);
        var cmd = new CreatePermissionCommand("ADMIN:READ", "Read Permission", "API", ApiUrl: "/api/test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePermission_Valid_ShouldSucceed()
    {
        _repo.Setup(r => r.CodeExistsAsync("NEW:PERM", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new CreatePermissionCommandHandler(_repo.Object, _createLog.Object);
        var cmd = new CreatePermissionCommand("NEW:PERM", "New Permission", "API",
            ApiMethod: "GET", ApiUrl: "/api/new");

        var result = await handler.Handler(cmd, CancellationToken.None);

        Assert.Equal("NEW:PERM", result.PermissionCode);
        Assert.NotEqual(Guid.Empty, result.PermissionId);
        _repo.Verify(r => r.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SavaEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePermission_NotFound_ShouldThrow()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        var handler = new UpdatePermissionCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdatePermissionCommand(Guid.NewGuid(), "Updated", "MENU", MenuPath: "/menu");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePermission_Valid_ShouldSucceed()
    {
        var existing = new Permission("ADMIN:READ", "Old Name", "API", ApiUrl: "/old");
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = new UpdatePermissionCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdatePermissionCommand(Guid.NewGuid(), "New Name", "MENU", MenuPath: "/new-menu");

        var result = await handler.Handler(cmd, CancellationToken.None);

        Assert.True(result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePermission_NotFound_ShouldThrow()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new DeletePermissionCommandHandler(_repo.Object, _deleteLog.Object);
        var cmd = new DeletePermissionCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePermission_Valid_ShouldSoftDelete()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new DeletePermissionCommandHandler(_repo.Object, _deleteLog.Object);
        var cmd = new DeletePermissionCommand(Guid.NewGuid());

        var result = await handler.Handler(cmd, CancellationToken.None);
        Assert.True(result);
    }
}

public class RoleGroupCommandHandlerTests
{
    private readonly Mock<IRoleGroupRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ILogger<CreateRoleGroupCommandHandler>> _createLog;
    private readonly Mock<ILogger<UpdateRoleGroupCommandHandler>> _updateLog;
    private readonly Mock<ILogger<DeleteRoleGroupCommandHandler>> _deleteLog;

    public RoleGroupCommandHandlerTests()
    {
        _repo = new Mock<IRoleGroupRepository>();
        _uow = new Mock<IUnitOfWork>();
        _repo.Setup(r => r.UnitOfWork).Returns(_uow.Object);
        _createLog = new Mock<ILogger<CreateRoleGroupCommandHandler>>();
        _updateLog = new Mock<ILogger<UpdateRoleGroupCommandHandler>>();
        _deleteLog = new Mock<ILogger<DeleteRoleGroupCommandHandler>>();
    }

    [Fact]
    public async Task CreateRoleGroup_Valid_ShouldSucceed()
    {
        var handler = new CreateRoleGroupCommandHandler(_repo.Object, _createLog.Object);
        var cmd = new CreateRoleGroupCommand("Admin Group", "ADMIN_GROUP");

        var result = await handler.Handler(cmd, CancellationToken.None);

        Assert.Equal("Admin Group", result.RoleGroupName);
        Assert.Equal("ADMIN_GROUP", result.RoleGroupCode);
        _uow.Verify(u => u.SavaEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleGroup_NotFound_ShouldThrow()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoleGroup>());

        var handler = new UpdateRoleGroupCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdateRoleGroupCommand(Guid.NewGuid(), "New Name", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRoleGroup_Valid_ShouldSucceed()
    {
        var existing = new RoleGroup("Old Group", "OLD_GROUP");
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoleGroup> { existing });

        var handler = new UpdateRoleGroupCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdateRoleGroupCommand(existing.RoleGroupGuid, "New Group", null);

        var result = await handler.Handler(cmd, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteRoleGroup_Valid_ShouldSoftDelete()
    {
        var existing = new RoleGroup("Group", "GROUP");
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RoleGroup> { existing });

        var handler = new DeleteRoleGroupCommandHandler(_repo.Object, _deleteLog.Object);
        var cmd = new DeleteRoleGroupCommand(existing.RoleGroupGuid);

        var result = await handler.Handler(cmd, CancellationToken.None);
        Assert.True(result);
        Assert.True(existing.IsDeleted);
        _uow.Verify(u => u.SavaEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RoleCommandHandlerTests
{
    private readonly Mock<IUserRoleRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<ILogger<CreateRoleCommandHandler>> _createLog;
    private readonly Mock<ILogger<UpdateRoleCommandHandler>> _updateLog;
    private readonly Mock<ILogger<DeleteRoleCommandHandler>> _deleteLog;

    public RoleCommandHandlerTests()
    {
        _repo = new Mock<IUserRoleRepository>();
        _uow = new Mock<IUnitOfWork>();
        _repo.Setup(r => r.UnitOfWork).Returns(_uow.Object);
        _createLog = new Mock<ILogger<CreateRoleCommandHandler>>();
        _updateLog = new Mock<ILogger<UpdateRoleCommandHandler>>();
        _deleteLog = new Mock<ILogger<DeleteRoleCommandHandler>>();
    }

    [Fact]
    public async Task CreateRole_DuplicateName_ShouldThrow()
    {
        _repo.Setup(r => r.IsUserRoleAsync("TestRole")).ReturnsAsync(true);

        var handler = new CreateRoleCommandHandler(_repo.Object, _createLog.Object);
        var cmd = new CreateRoleCommand("TestRole", "TEST_ROLE");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRole_Valid_ShouldSucceed()
    {
        _repo.Setup(r => r.IsUserRoleAsync(It.IsAny<string>())).ReturnsAsync(false);

        var handler = new CreateRoleCommandHandler(_repo.Object, _createLog.Object);
        var cmd = new CreateRoleCommand("NewRole", "NEW_ROLE", RoleAuthority.User, "Test attribute");

        var result = await handler.Handler(cmd, CancellationToken.None);

        Assert.Equal("NewRole", result.RoleName);
        Assert.Equal("NEW_ROLE", result.RoleCode);
        _repo.Verify(r => r.AddByUserRoleAsync(It.IsAny<Roles>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRole_NotFound_ShouldThrow()
    {
        _repo.Setup(r => r.FindByUserRoleAsync(It.IsAny<Guid>())).ReturnsAsync((Roles?)null);

        var handler = new UpdateRoleCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdateRoleCommand(Guid.NewGuid(), "Updated", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handler(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRole_Valid_ShouldSucceed()
    {
        var existing = new Roles("OldRole", "OLD_ROLE");
        _repo.Setup(r => r.FindByUserRoleAsync(It.IsAny<Guid>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpByUserRoleAsync(It.IsAny<Roles>())).ReturnsAsync(true);

        var handler = new UpdateRoleCommandHandler(_repo.Object, _updateLog.Object);
        var cmd = new UpdateRoleCommand(existing.RoleGuid, "NewRole", "Updated attribute");

        var result = await handler.Handler(cmd, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteRole_Valid_ShouldSoftDelete()
    {
        var existing = new Roles("ToDelete", "TO_DELETE");
        _repo.Setup(r => r.FindByUserRoleAsync(It.IsAny<Guid>())).ReturnsAsync(existing);
        _repo.Setup(r => r.UpByUserRoleAsync(It.IsAny<Roles>())).ReturnsAsync(true);

        var handler = new DeleteRoleCommandHandler(_repo.Object, _deleteLog.Object);
        var cmd = new DeleteRoleCommand(existing.RoleGuid);

        var result = await handler.Handler(cmd, CancellationToken.None);
        Assert.True(result);
        _repo.Verify(r => r.UpByUserRoleAsync(It.IsAny<Roles>()), Times.Once);
    }
}

/// <summary>
/// 幂等性测试：验证 IdentifiedCommand 包装的重复请求处理
/// </summary>
public class IdempotencyTests
{
    private readonly Mock<IRequestManagement> _reqMgmt;
    private readonly Mock<INotMediator> _mediator;

    public IdempotencyTests()
    {
        _reqMgmt = new Mock<IRequestManagement>();
        _mediator = new Mock<INotMediator>();
    }

    [Fact]
    public async Task IdentifiedCommand_DuplicateRequest_ReturnsCachedResult()
    {
        // Arrange: simulate first request already stored
        var storedResult = new CreatePermissionResult(Guid.NewGuid(), "TEST:PERM");
        var requestId = Guid.CreateVersion7();
        var cmd = new CreatePermissionCommand("TEST:PERM", "Test", "API");

        _reqMgmt.Setup(r => r.ExistAsync(requestId)).ReturnsAsync(true);
        _reqMgmt.Setup(r => r.GetResultAsync<CreatePermissionResult>(requestId))
            .ReturnsAsync(storedResult);

        var handler = new CreatePermissionIdentifiedCommandHandler(
            Mock.Of<ILogger<IdentifiedCommandHandler<CreatePermissionCommand, CreatePermissionResult>>>(),
            _mediator.Object, _reqMgmt.Object);

        var identCmd = new IdentifiedCommand<CreatePermissionCommand, CreatePermissionResult>(requestId, cmd);

        // Act
        var result = await handler.Handler(identCmd, CancellationToken.None);

        // Assert: returns cached result without calling mediator
        Assert.Equal(storedResult.PermissionId, result.PermissionId);
        Assert.Equal(storedResult.PermissionCode, result.PermissionCode);
        _mediator.Verify(m => m.SendAsync(It.IsAny<IdentifiedCommand<CreatePermissionCommand, CreatePermissionResult>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdentifiedCommand_NewRequest_DelegatesToInnerHandler()
    {
        // Arrange
        var requestId = Guid.CreateVersion7();
        var cmd = new CreatePermissionCommand("NEW:PERM", "New", "API");
        var innerResult = new CreatePermissionResult(Guid.NewGuid(), "NEW:PERM");

        _reqMgmt.Setup(r => r.ExistAsync(requestId)).ReturnsAsync(false);
        _mediator.Setup(m => m.SendAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(innerResult);

        var handler = new CreatePermissionIdentifiedCommandHandler(
            Mock.Of<ILogger<IdentifiedCommandHandler<CreatePermissionCommand, CreatePermissionResult>>>(),
            _mediator.Object, _reqMgmt.Object);

        var identCmd = new IdentifiedCommand<CreatePermissionCommand, CreatePermissionResult>(requestId, cmd);

        // Act
        var result = await handler.Handler(identCmd, CancellationToken.None);

        // Assert
        Assert.Equal(innerResult.PermissionCode, result.PermissionCode);
        _mediator.Verify(m => m.SendAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IdentifiedCommand_AllCreateDuplicateResults_ReturnEmpty()
    {
        // Verify all duplicate-result factories return empty/invalid values
        var perm = new CreatePermissionIdentifiedCommandHandler(null!, null!, null!)
            .CreateResultForDuplicateRequest();
        var rg = new CreateRoleGroupIdentifiedCommandHandler(null!, null!, null!)
            .CreateResultForDuplicateRequest();
        var role = new CreateRoleIdentifiedCommandHandler(null!, null!, null!)
            .CreateResultForDuplicateRequest();

        Assert.Equal(Guid.Empty, perm.PermissionId);
        Assert.Equal(Guid.Empty, rg.RoleGroupGuid);
        Assert.Equal(Guid.Empty, role.RoleGuid);
    }
}

public class PermissionEntityTests
{
    [Fact]
    public void Permission_Constructor_ShouldSetProperties()
    {
        var p = new Permission("ADMIN:READ", "Admin Read", "API",
            apiMethod: "GET", apiUrl: "/api/admin");

        Assert.Equal("ADMIN:READ", p.PermissionCode);
        Assert.Equal("Admin Read", p.PermissionName);
        Assert.Equal("API", p.PermissionType);
        Assert.Equal("GET", p.ApiMethod);
        Assert.Equal("/api/admin", p.ApiUrl);
        Assert.False(p.IsDeleted);
    }

    [Fact]
    public void Permission_ChangePermission_ShouldUpdate()
    {
        var p = new Permission("OLD:CODE", "Old Name", "API", ApiUrl: "/old");

        p.ChangePermission("", "New Name", "MENU", "/menu", "", "");

        Assert.Equal("OLD:CODE", p.PermissionCode); // code unchanged when empty
        Assert.Equal("New Name", p.PermissionName);
        Assert.Equal("MENU", p.PermissionType);
        Assert.Equal("/menu", p.MenuPath);
    }

    [Fact]
    public void Permission_SoftDelete_ShouldMarkDeleted()
    {
        var p = new Permission("CODE", "Name", "API");

        p.SoftDelete(true);
        Assert.True(p.IsDeleted);

        p.SoftDelete(false);
        Assert.False(p.IsDeleted);
    }

    [Fact]
    public void RoleGroup_Constructor_ShouldSetProperties()
    {
        var rg = new RoleGroup("Admin Group", "ADMIN_GROUP");

        Assert.Equal("Admin Group", rg.RoleGroupName);
        Assert.Equal("ADMIN_GROUP", rg.RoleGroupCode);
        Assert.False(rg.IsDeleted);
    }

    [Fact]
    public void RoleGroup_SoftDelete_ShouldMarkDeleted()
    {
        var rg = new RoleGroup("Test", "TEST");

        rg.SoftDelete(true);
        Assert.True(rg.IsDeleted);
    }

    [Fact]
    public void Roles_Constructor_ShouldSetProperties()
    {
        var r = new Roles("TestRole", "TEST_ROLE", RoleAuthority.User, RoleStatus.Normal, "desc");

        Assert.Equal("TestRole", r.RoleName);
        Assert.Equal("TEST_ROLE", r.RoleCode);
        Assert.Equal(RoleAuthority.User, r.RoleAuthority);
        Assert.False(r.IsDeleted);
    }

    [Fact]
    public void Roles_UpdateRoleInfo_ShouldUpdate()
    {
        var r = new Roles("Old", "OLD");

        r.UpdateRoleInfo("New Name", "New attribute");
        Assert.Equal("New Name", r.RoleName);
        Assert.Equal("New attribute", r.Attribute);
    }

    [Fact]
    public void Roles_ResetByRoleStatus_ShouldChangeStatus()
    {
        var r = new Roles("Test", "TEST");

        r.ResetByRoleStatus(RoleStatus.Disabled);
        Assert.Equal(RoleStatus.Disabled, r.RoleStatus);
    }

    [Fact]
    public void Commands_ShouldImplementILoggableCommand()
    {
        var createPerm = (ILoggableCommand)new CreatePermissionCommand("CODE", "Name", "API");
        var updatePerm = (ILoggableCommand)new UpdatePermissionCommand(Guid.NewGuid(), "Name", "API");
        var deletePerm = (ILoggableCommand)new DeletePermissionCommand(Guid.NewGuid());

        Assert.Equal("PermissionCode", createPerm.IdProperty);
        Assert.Equal("PermissionId", updatePerm.IdProperty);
        Assert.Equal("PermissionId", deletePerm.IdProperty);
    }
}
