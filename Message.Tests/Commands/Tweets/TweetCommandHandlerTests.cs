using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Tweets;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Tweets;

/// <summary>
/// 推文命令处理程序单元测试。
/// 覆盖：创建推文（返回新推文 ID）、点赞（含已点赞异常）、删除（权限校验）。
/// </summary>
[TestFixture]
public class TweetCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TweetGuid = Guid.NewGuid();

    private Mock<ITweetRepository> _tweetRepository = null!;
    private Mock<ITweetInteractionRepository> _interactionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _tweetRepository = new Mock<ITweetRepository>();
        _interactionRepository = new Mock<ITweetInteractionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _tweetRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
    }

    // ---------- CreateTweetCommandHandler ----------

    [Test]
    public async Task CreateTweet_应创建推文并返回新推文ID()
    {
        Tweet? added = null;
        _tweetRepository.Setup(r => r.AddAsync(It.IsAny<Tweet>()))
            .Callback<Tweet>(t => added = t)
            .ReturnsAsync((Tweet t) => t);

        var sensitiveWordFilter = new Mock<ISensitiveWordFilter>();
        sensitiveWordFilter.Setup(s => s.FilterAsync(It.IsAny<string>()))
            .ReturnsAsync(new FilterResult(true, Array.Empty<string>()));
        var imageModeration = new Mock<IImageModerationService>();

        var handler = new CreateTweetCommandHandler(
            _tweetRepository.Object,
            sensitiveWordFilter.Object,
            imageModeration.Object,
            new Mock<ILogger<CreateTweetCommandHandler>>().Object);

        var result = await handler.Handler(
            new CreateTweetCommand(UserId, "第一条推文", null, null, Visibility.Followers),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.TweetGuid));
            Assert.That(added.Visibility, Is.EqualTo(Visibility.Followers));
            Assert.That(added.AuthorGuid, Is.EqualTo(UserId));
            _tweetRepository.Verify(r => r.AddAsync(It.IsAny<Tweet>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- LikeTweetCommandHandler ----------

    [Test]
    public async Task LikeTweet_应创建互动并更新推文点赞数()
    {
        var tweet = Tweet.Create(UserId, "内容");
        _interactionRepository.Setup(r => r.ExistsAsync(TweetGuid, UserId, InteractionType.Like))
            .ReturnsAsync(false);
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _interactionRepository.Setup(r => r.AddAsync(It.IsAny<TweetInteraction>()))
            .ReturnsAsync((TweetInteraction i) => i);
        _tweetRepository.Setup(r => r.UpdateAsync(It.IsAny<Tweet>()))
            .ReturnsAsync((Tweet t) => t);

        var handler = new LikeTweetCommandHandler(
            _tweetRepository.Object,
            _interactionRepository.Object,
            new Mock<ILogger<LikeTweetCommandHandler>>().Object);

        var result = await handler.Handler(new LikeTweetCommand(TweetGuid, UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(tweet.LikeCount, Is.EqualTo(1));
            _interactionRepository.Verify(r => r.AddAsync(
                It.Is<TweetInteraction>(i => i.TweetGuid == TweetGuid && i.UserGuid == UserId &&
                                             i.Type == InteractionType.Like)), Times.Once);
            _tweetRepository.Verify(r => r.UpdateAsync(tweet), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task LikeTweet_已点赞时_应抛出InvalidOperationException()
    {
        _interactionRepository.Setup(r => r.ExistsAsync(TweetGuid, UserId, InteractionType.Like))
            .ReturnsAsync(true);

        var handler = new LikeTweetCommandHandler(
            _tweetRepository.Object,
            _interactionRepository.Object,
            new Mock<ILogger<LikeTweetCommandHandler>>().Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(new LikeTweetCommand(TweetGuid, UserId), CancellationToken.None));

        _interactionRepository.Verify(r => r.AddAsync(It.IsAny<TweetInteraction>()), Times.Never);
    }

    // ---------- DeleteTweetCommandHandler ----------

    [Test]
    public async Task DeleteTweet_作者本人_应删除并返回true()
    {
        var tweet = Tweet.Create(UserId, "内容");
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _tweetRepository.Setup(r => r.DeleteAsync(TweetGuid)).Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUserService>();

        var handler = new DeleteTweetCommandHandler(
            _tweetRepository.Object,
            currentUser.Object,
            new Mock<ILogger<DeleteTweetCommandHandler>>().Object);

        var result = await handler.Handler(new DeleteTweetCommand(TweetGuid, UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _tweetRepository.Verify(r => r.DeleteAsync(TweetGuid), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task DeleteTweet_非作者且非管理员时_应抛出UnauthorizedAccessException()
    {
        var tweet = Tweet.Create(UserId, "内容");
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.IsAdmin()).Returns(false);

        var handler = new DeleteTweetCommandHandler(
            _tweetRepository.Object,
            currentUser.Object,
            new Mock<ILogger<DeleteTweetCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new DeleteTweetCommand(TweetGuid, Guid.NewGuid()), CancellationToken.None));

        _tweetRepository.Verify(r => r.DeleteAsync(TweetGuid), Times.Never);
    }
}
