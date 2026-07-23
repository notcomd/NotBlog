using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotMediator;

namespace Message.Infrastructure.Tests;

/// <summary>
///   验证 GroupRepository 的 GetByIdWithMembersAsync 方法和新增的聚合根查询功能
/// </summary>
public class GroupRepositoryTests : IAsyncLifetime
{
    private readonly IGroupRepository _repository;
    private readonly MessageDbContext _context;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private Guid _groupId;

    public GroupRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MessageDbContext>()
            .UseInMemoryDatabase($"GroupTestDb_{Guid.NewGuid()}")
            .Options;

        var notMediatorMock = new Mock<INotMediator>();
        _context = new MessageDbContext(options, notMediatorMock.Object);
        _repository = new Message.Infrastructure.Repository.GroupRepository(_context);
    }

    public async Task InitializeAsync()
    {
        var group = new Group(_ownerId, "测试群组", 500, true);
        group.AddMember(_memberId, GroupMemberRole.Member);
        _groupId = group.GroupId;

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_Should_Return_Group_With_Members()
    {
        var group = await _repository.GetByIdWithMembersAsync(_groupId);

        Assert.NotNull(group);
        Assert.NotEmpty(group!.Members);
        Assert.Contains(group.Members, m => m.UserId == _ownerId);
        Assert.Contains(group.Members, m => m.UserId == _memberId);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Group_Without_Members()
    {
        var options = new DbContextOptionsBuilder<MessageDbContext>()
            .UseInMemoryDatabase($"GroupTestDb_NoMembers_{Guid.NewGuid()}")
            .Options;

        var notMediatorMock = new Mock<INotMediator>();
        await using var freshContext = new MessageDbContext(options, notMediatorMock.Object);
        var group = new Group(_ownerId, "独立测试群");
        group.AddMember(_memberId, GroupMemberRole.Member);
        freshContext.Groups.Add(group);
        await freshContext.SaveChangesAsync();

        // Detach to simulate fresh query without tracking
        freshContext.Entry(group).State = EntityState.Detached;

        var repo = new Message.Infrastructure.Repository.GroupRepository(freshContext);

        var loaded = await repo.GetByIdAsync(group.GroupId);

        Assert.NotNull(loaded);
        Assert.Empty(loaded!.Members);
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_NonExistent_Should_Return_Null()
    {
        var group = await _repository.GetByIdWithMembersAsync(Guid.NewGuid());

        Assert.Null(group);
    }

    [Fact]
    public async Task TransferOwnership_Through_GetByIdWithMembers_Should_Work()
    {
        var group = await _repository.GetByIdWithMembersAsync(_groupId);
        Assert.NotNull(group);

        group!.TransferOwnership(_memberId);

        await _context.SaveChangesAsync();

        var updated = await _repository.GetByIdWithMembersAsync(_groupId);
        Assert.NotNull(updated);
        Assert.Equal(_memberId, updated!.OwnerId);
    }

    [Fact]
    public async Task AddMember_Through_Aggregate_Should_Persist()
    {
        // Use a completely isolated context to avoid cross-test contamination
        var options = new DbContextOptionsBuilder<MessageDbContext>()
            .UseInMemoryDatabase($"GroupTestDb_AddMember_{Guid.NewGuid()}")
            .Options;
        var notMediatorMock = new Mock<INotMediator>();
        await using var context = new MessageDbContext(options, notMediatorMock.Object);
        var repo = new Message.Infrastructure.Repository.GroupRepository(context);

        var group = new Group(_ownerId, "独立测试群");
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Load with members tracked
        var loaded = await repo.GetByIdWithMembersAsync(group.GroupId);
        Assert.NotNull(loaded);

        var newMemberId = Guid.NewGuid();
        loaded!.AddMember(newMemberId, GroupMemberRole.Member);

        // EF Core InMemory provider doesn't auto-detect new child entities
        // with pre-set keys; explicitly mark the new member as Added
        var newMember = loaded.GetMember(newMemberId);
        Assert.NotNull(newMember);
        context.Entry(newMember).State = EntityState.Added;

        await context.SaveChangesAsync();

        // Verify persistence via fresh load
        context.ChangeTracker.Clear();
        var reloaded = await repo.GetByIdWithMembersAsync(group.GroupId);
        Assert.NotNull(reloaded);
        Assert.Contains(reloaded!.Members, m => m.UserId == newMemberId);
    }

    [Fact]
    public async Task RemoveMember_Through_Aggregate_Should_Persist()
    {
        var group = await _repository.GetByIdWithMembersAsync(_groupId);
        Assert.NotNull(group);

        group!.RemoveMember(_memberId);

        await _context.SaveChangesAsync();

        var reloaded = await _repository.GetByIdWithMembersAsync(_groupId);
        Assert.NotNull(reloaded);
        Assert.DoesNotContain(reloaded!.Members, m => m.UserId == _memberId);
    }
}
