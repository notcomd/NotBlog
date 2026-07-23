using Message.Domain.Entities.Tweet;
using Message.Domain.IRepository;
using Message.Domain.SeedWork;

namespace Message.Infrastructure.Tests;

/// <summary>
///   验证聚合根标记和仓储接口继承关系的正确性
/// </summary>
public class AggregateRootMarkingTests
{
    [Fact]
    public void TweetReport_Should_Implement_IAggregateRoot()
    {
        var report = TweetReport.Create(
            Guid.NewGuid(),
            Message.Domain.Enums.ReportTargetType.Tweet,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "测试举报",
            Message.Domain.Enums.ReportCategory.Spam,
            new List<string>());

        Assert.IsAssignableFrom<IAggregateRoot>(report);
    }

    [Fact]
    public void Comment_Should_Implement_IAggregateRoot()
    {
        var comment = Comment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "测试评论");

        Assert.IsAssignableFrom<IAggregateRoot>(comment);
    }

    [Fact]
    public void Group_Should_Implement_IAggregateRoot()
    {
        var group = new Message.Domain.Entities.Group.Group(Guid.NewGuid(), "测试群组");

        Assert.IsAssignableFrom<IAggregateRoot>(group);
    }

    [Fact]
    public void GroupMember_Should_Not_Implement_IAggregateRoot()
    {
        var member = new Message.Domain.Entities.Group.GroupMember(Guid.NewGuid(), Guid.NewGuid(), Message.Domain.Enums.GroupMemberRole.Member);

        Assert.IsNotAssignableFrom<IAggregateRoot>(member);
    }

    [Fact]
    public void TweetInteraction_Should_Not_Implement_IAggregateRoot()
    {
        var interaction = TweetInteraction.Create(Guid.NewGuid(), Guid.NewGuid(), Message.Domain.Enums.InteractionType.Like);

        Assert.IsNotAssignableFrom<IAggregateRoot>(interaction);
    }

    [Fact]
    public void TweetNotification_Should_Not_Implement_IAggregateRoot()
    {
        var notification = TweetNotification.Create(
            Guid.NewGuid(),
            Message.Domain.Enums.NotificationType.TweetLiked,
            "标题",
            "内容");

        Assert.IsNotAssignableFrom<IAggregateRoot>(notification);
    }

    /// <summary>
    ///   ICommentRepository 应继承 IRepository<Comment>（Comment 现在是聚合根）
    /// </summary>
    [Fact]
    public void ICommentRepository_Should_Inherit_From_IRepository_Of_Comment()
    {
        var type = typeof(ICommentRepository);

        Assert.True(typeof(IRepository<Comment>).IsAssignableFrom(type),
            "ICommentRepository 应继承 IRepository<Comment>");
    }

    /// <summary>
    ///   ITweetReportRepository 应继承 IRepository<TweetReport>（TweetReport 现在是聚合根）
    /// </summary>
    [Fact]
    public void ITweetReportRepository_Should_Inherit_From_IRepository_Of_TweetReport()
    {
        var type = typeof(Message.Domain.IRepository.ITweetReportRepository);

        Assert.True(typeof(IRepository<TweetReport>).IsAssignableFrom(type),
            "ITweetReportRepository 应继承 IRepository<TweetReport>");
    }

    /// <summary>
    ///   TweetReport 作为聚合根，应能发出领域事件
    /// </summary>
    [Fact]
    public void TweetReport_As_AggregateRoot_Should_Emit_DomainEvents()
    {
        var report = TweetReport.Create(
            Guid.NewGuid(),
            Message.Domain.Enums.ReportTargetType.Tweet,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "测试举报内容",
            Message.Domain.Enums.ReportCategory.Harassment,
            new List<string> { "http://example.com/evidence.jpg" });

        Assert.NotEmpty(report.DomainEvents);
    }

    /// <summary>
    ///   Comment 作为聚合根，应能发出领域事件
    /// </summary>
    [Fact]
    public void Comment_As_AggregateRoot_Should_Emit_DomainEvents()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), "测试评论内容");

        Assert.NotEmpty(comment.DomainEvents);
    }
}
