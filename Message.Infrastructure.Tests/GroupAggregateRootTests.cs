using Message.Domain.Entities.Group;
using Message.Domain.Enums;

namespace Message.Infrastructure.Tests;

/// <summary>
///   验证 Group 聚合根新增的成员操作方法
/// </summary>
public class GroupAggregateRootTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    private Group CreateGroup()
    {
        var group = new Group(_ownerId, "测试群组", 500, true);
        group.AddMember(_memberId, GroupMemberRole.Member);
        return group;
    }

    [Fact]
    public void PromoteMember_Should_Change_Role_To_Admin()
    {
        var group = CreateGroup();

        group.PromoteMember(_memberId);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.Equal(GroupMemberRole.Admin, member!.Role);
    }

    [Fact]
    public void DemoteMember_Should_Change_Role_From_Admin_To_Member()
    {
        var group = CreateGroup();
        group.PromoteMember(_memberId);

        group.DemoteMember(_memberId);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.Equal(GroupMemberRole.Member, member!.Role);
    }

    [Fact]
    public void MuteMember_Should_Set_IsMuted_True_And_Set_EndTime()
    {
        var group = CreateGroup();
        var duration = TimeSpan.FromHours(1);

        group.MuteMember(_memberId, duration);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.True(member!.IsMuted);
        Assert.NotNull(member.MuteEndTime);
    }

    [Fact]
    public void UnmuteMember_Should_Set_IsMuted_False()
    {
        var group = CreateGroup();
        group.MuteMember(_memberId, TimeSpan.FromHours(1));

        group.UnmuteMember(_memberId);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.False(member!.IsMuted);
        Assert.Null(member.MuteEndTime);
    }

    [Fact]
    public void BanMember_Should_Set_IsBanned_True()
    {
        var group = CreateGroup();

        group.BanMember(_memberId);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.True(member!.IsBanned);
    }

    [Fact]
    public void UnbanMember_Should_Set_IsBanned_False()
    {
        var group = CreateGroup();
        group.BanMember(_memberId);

        group.UnbanMember(_memberId);

        var member = group.GetMember(_memberId);
        Assert.NotNull(member);
        Assert.False(member!.IsBanned);
    }

    [Fact]
    public void PromoteMember_NonExistent_Should_Throw_KeyNotFound()
    {
        var group = CreateGroup();

        Assert.Throws<KeyNotFoundException>(() => group.PromoteMember(Guid.NewGuid()));
    }

    [Fact]
    public void MuteMember_NonExistent_Should_Throw_KeyNotFound()
    {
        var group = CreateGroup();

        Assert.Throws<KeyNotFoundException>(() => group.MuteMember(Guid.NewGuid(), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void BanMember_NonExistent_Should_Throw_KeyNotFound()
    {
        var group = CreateGroup();

        Assert.Throws<KeyNotFoundException>(() => group.BanMember(Guid.NewGuid()));
    }

    [Fact]
    public void Member_Methods_Should_Throw_When_Group_Dismissed()
    {
        var group = CreateGroup();
        group.Dismiss();

        Assert.Throws<InvalidOperationException>(() => group.PromoteMember(_memberId));
        Assert.Throws<InvalidOperationException>(() => group.DemoteMember(_memberId));
        Assert.Throws<InvalidOperationException>(() => group.MuteMember(_memberId, TimeSpan.FromHours(1)));
        Assert.Throws<InvalidOperationException>(() => group.UnmuteMember(_memberId));
        Assert.Throws<InvalidOperationException>(() => group.BanMember(_memberId));
        Assert.Throws<InvalidOperationException>(() => group.UnbanMember(_memberId));
    }

    [Fact]
    public void GetByIdWithMembers_Should_Return_Group_With_Members_Populated()
    {
        var group = CreateGroup();

        Assert.NotEmpty(group.Members);
        Assert.Contains(group.Members, m => m.UserId == _memberId);
        Assert.Contains(group.Members, m => m.UserId == _ownerId);
    }

    [Fact]
    public void PromoteMember_PromotesOwner_Should_Throw_InvalidOperation()
    {
        var group = CreateGroup();

        // GroupMember.PromoteToAdmin throws when role is Owner
        Assert.Throws<InvalidOperationException>(() => group.PromoteMember(_ownerId));
    }
}
