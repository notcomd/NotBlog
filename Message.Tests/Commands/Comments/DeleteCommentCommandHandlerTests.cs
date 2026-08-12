using Message.Domain.Entities.Community;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Comments;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Comments;

/// <summary>
/// 删除评论命令处理程序单元测试（频道内容管理）：
/// 评论作者本人可删；圈子帖评论允许圈主/圈管理员删除；普通成员不可删他人评论。
/// </summary>
[TestFixture]
public class DeleteCommentCommandHandlerTests
{
    private static readonly Guid TweetGuid = Guid.NewGuid();
    private static readonly Guid CommentGuid = Guid.NewGuid();
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid CircleGuid = Guid.NewGuid();

    private Mock<ICommentRepository> _commentRepository = null!;
    private Mock<ITweetRepository> _tweetRepository = null!;
    private Mock<ICircleRepository> _circleRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _commentRepository = new Mock<ICommentRepository>();
        _tweetRepository = new Mock<ITweetRepository>();
        _circleRepository = new Mock<ICircleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _commentRepository.Setup(r => r.DeleteAsync(CommentGuid)).Returns(Task.CompletedTask);
        _commentRepository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
    }

    private static Comment CreateComment(Guid userGuid) => Comment.Create(TweetGuid, userGuid, "测试评论");

    private DeleteCommentCommandHandler CreateHandler(ICurrentUserService currentUser)
        => new(_commentRepository.Object, _tweetRepository.Object, _circleRepository.Object,
            currentUser, new Mock<ILogger<DeleteCommentCommandHandler>>().Object);

    private static Mock<ICurrentUserService> AdminUser()
    {
        var u = new Mock<ICurrentUserService>();
        u.Setup(x => x.IsAdmin()).Returns(true);
        return u;
    }

    private static Mock<ICurrentUserService> NormalUser()
    {
        var u = new Mock<ICurrentUserService>();
        u.Setup(x => x.IsAdmin()).Returns(false);
        return u;
    }

    [Test]
    public async Task DeleteComment_评论作者本人_应删除成功()
    {
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));

        var result = await CreateHandler(NormalUser().Object).Handler(
            new DeleteCommentCommand(CommentGuid, AuthorId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _commentRepository.Verify(r => r.DeleteAsync(CommentGuid), Times.Once);
        });
    }

    [Test]
    public async Task DeleteComment_全局管理员删除他人评论_应成功()
    {
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));

        var result = await CreateHandler(AdminUser().Object).Handler(
            new DeleteCommentCommand(CommentGuid, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteComment_圈主删除圈子帖下他人评论_应成功()
    {
        var circleOwner = Guid.NewGuid();
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));
        var tweet = Tweet.Create(AuthorId, "圈子帖", null, null, null, Visibility.Public, CircleGuid);
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, circleOwner))
            .ReturnsAsync(new CircleMember(CircleGuid, circleOwner, CircleMemberRole.Owner));

        var result = await CreateHandler(NormalUser().Object).Handler(
            new DeleteCommentCommand(CommentGuid, circleOwner), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _commentRepository.Verify(r => r.DeleteAsync(CommentGuid), Times.Once);
        });
    }

    [Test]
    public async Task DeleteComment_圈管理员删除圈子帖下他人评论_应成功()
    {
        var circleAdmin = Guid.NewGuid();
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));
        var tweet = Tweet.Create(AuthorId, "圈子帖", null, null, null, Visibility.Public, CircleGuid);
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, circleAdmin))
            .ReturnsAsync(new CircleMember(CircleGuid, circleAdmin, CircleMemberRole.Admin));

        var result = await CreateHandler(NormalUser().Object).Handler(
            new DeleteCommentCommand(CommentGuid, circleAdmin), CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteComment_普通成员删除他人评论_应拒绝()
    {
        var plainMember = Guid.NewGuid();
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));
        var tweet = Tweet.Create(AuthorId, "圈子帖", null, null, null, Visibility.Public, CircleGuid);
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, plainMember))
            .ReturnsAsync(new CircleMember(CircleGuid, plainMember, CircleMemberRole.Member));

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler(NormalUser().Object).Handler(
                new DeleteCommentCommand(CommentGuid, plainMember), CancellationToken.None));

        _commentRepository.Verify(r => r.DeleteAsync(CommentGuid), Times.Never);
    }

    [Test]
    public async Task DeleteComment_全局帖评论_非作者非管理员_应拒绝()
    {
        _commentRepository.Setup(r => r.GetByIdAsync(CommentGuid)).ReturnsAsync(CreateComment(AuthorId));
        var tweet = Tweet.Create(AuthorId, "全局帖");
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler(NormalUser().Object).Handler(
                new DeleteCommentCommand(CommentGuid, Guid.NewGuid()), CancellationToken.None));

        _commentRepository.Verify(r => r.DeleteAsync(CommentGuid), Times.Never);
    }
}
