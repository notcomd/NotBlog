using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Queries.Tweets;
using Moq;

namespace Message.Tests.Queries.Tweets;

/// <summary>
/// 推文详情查询处理程序单元测试。
/// 覆盖：推文不存在时返回空结果且不查询交互状态、推文存在时返回推文与三项交互状态。
/// </summary>
[TestFixture]
public class GetTweetDetailQueryHandlerTests
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
        _tweetRepository.Setup(r => r.UpdateAsync(It.IsAny<Tweet>()))
            .ReturnsAsync((Tweet t) => t);
    }

    [Test]
    public async Task GetTweetDetail_推文不存在时_应返回空结果且不查询交互状态()
    {
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync((Tweet?)null);

        var handler = new GetTweetDetailQueryHandler(_tweetRepository.Object, _interactionRepository.Object, new Mock<ICircleRepository>().Object);

        var result = await handler.Handler(new GetTweetDetailQuery(TweetGuid, UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tweet, Is.Null);
            Assert.That(result.IsLiked, Is.False);
            Assert.That(result.IsFavorited, Is.False);
            Assert.That(result.IsCoined, Is.False);
            _interactionRepository.Verify(r => r.ExistsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<InteractionType>()), Times.Never);
            _tweetRepository.Verify(r => r.UpdateAsync(It.IsAny<Tweet>()), Times.Never);
        });
    }

    [Test]
    public async Task GetTweetDetail_草稿推文_应返回推文与交互状态且不写库()
    {
        var tweet = Tweet.Create(UserId, "推文内容");
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _interactionRepository.Setup(r => r.ExistsAsync(TweetGuid, UserId, InteractionType.Like))
            .ReturnsAsync(true);
        _interactionRepository.Setup(r => r.ExistsAsync(TweetGuid, UserId, InteractionType.Favorite))
            .ReturnsAsync(false);
        _interactionRepository.Setup(r => r.ExistsAsync(TweetGuid, UserId, InteractionType.Coin))
            .ReturnsAsync(true);

        var handler = new GetTweetDetailQueryHandler(_tweetRepository.Object, _interactionRepository.Object, new Mock<ICircleRepository>().Object);

        var result = await handler.Handler(new GetTweetDetailQuery(TweetGuid, UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tweet, Is.SameAs(tweet));
            Assert.That(result.IsLiked, Is.True);
            Assert.That(result.IsFavorited, Is.False);
            Assert.That(result.IsCoined, Is.True);
            _tweetRepository.Verify(r => r.UpdateAsync(It.IsAny<Tweet>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _interactionRepository.Verify(r => r.ExistsAsync(
                TweetGuid, UserId, InteractionType.Like), Times.Once);
            _interactionRepository.Verify(r => r.ExistsAsync(
                TweetGuid, UserId, InteractionType.Favorite), Times.Once);
            _interactionRepository.Verify(r => r.ExistsAsync(
                TweetGuid, UserId, InteractionType.Coin), Times.Once);
        });
    }

    [Test]
    public async Task GetTweetDetail_已审核推文_应增加浏览量并持久化()
    {
        var tweet = Tweet.Create(UserId, "推文内容");
        tweet.Publish();
        tweet.Approve(Guid.NewGuid());
        _tweetRepository.Setup(r => r.GetByIdAsync(TweetGuid)).ReturnsAsync(tweet);
        _interactionRepository.Setup(r => r.ExistsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<InteractionType>()))
            .ReturnsAsync(false);

        var handler = new GetTweetDetailQueryHandler(_tweetRepository.Object, _interactionRepository.Object, new Mock<ICircleRepository>().Object);

        var result = await handler.Handler(new GetTweetDetailQuery(TweetGuid, UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Tweet, Is.SameAs(tweet));
            Assert.That(tweet.ViewCount, Is.EqualTo(1));
            _tweetRepository.Verify(r => r.UpdateAsync(tweet), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}
