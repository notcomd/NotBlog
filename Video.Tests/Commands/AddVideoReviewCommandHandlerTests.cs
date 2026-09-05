using Notcomd.EventBus.Core;
using Video.Tests.Commands;

namespace Video.Tests.Commands;

/// <summary>
/// 添加视频评论命令处理器单元测试 — 覆盖互动实施文档 D-8：
/// 顶级评论 → 通知视频作者；回复评论 → 通知被回复评论作者；评论正文预览截断。
/// </summary>
[TestFixture]
public class AddVideoReviewCommandHandlerTests
{
    private static readonly Guid AuthorId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid VideoId = Guid.NewGuid();
    private static readonly Guid ParentReviewId = Guid.NewGuid();

    private Mock<IVideoRepository> _videoRepo = null!;
    private Mock<IVideoCacheService> _cache = null!;
    private Mock<IEventBus> _bus = null!;
    private List<IntegrationEvent> _published = null!;
    private AddVideoReviewCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        var video = VideoFixtures.BuildVideo(AuthorId, VideoId);
        // 父评论：由作者发布，回复场景下应通知到该评论作者
        VideoFixtures.AddReview(video, ParentReviewId, AuthorId, null, "父评论内容");
        (_videoRepo, _) = VideoFixtures.BuildVideoRepository(video);
        _cache = VideoFixtures.BuildCacheService();
        (_bus, _published) = VideoFixtures.BuildEventBus();
        _handler = new AddVideoReviewCommandHandler(
            _cache.Object, _videoRepo.Object, _bus.Object,
            NullLogger<AddVideoReviewCommandHandler>.Instance);
    }

    private Task<bool> HandleAsync(Guid? rootReview, string? body, Guid? actor = null)
        => _handler.Handler(
            new AddVideoReviewCommand(Guid.CreateVersion7(), VideoId, actor ?? ActorId, rootReview, body, null),
            default);

    private VideoCommentPublishedIntegrationEvent? SingleCommentEvent()
        => _published.OfType<VideoCommentPublishedIntegrationEvent>().SingleOrDefault();

    [Test]
    public async Task 顶级评论_通知视频作者()
    {
        var ok = await HandleAsync(rootReview: null, body: "新评论");

        Assert.That(ok, Is.True);
        var evt = SingleCommentEvent();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TargetUserId, Is.EqualTo(AuthorId));
        Assert.That(evt.ActorUserId, Is.EqualTo(ActorId));
        Assert.That(evt.RootReviewGuid, Is.Null);
        Assert.That(evt.CommentPreview, Is.EqualTo("新评论"));
    }

    [Test]
    public async Task 回复评论_通知被回复评论作者()
    {
        var ok = await HandleAsync(rootReview: ParentReviewId, body: "回复内容");

        Assert.That(ok, Is.True);
        var evt = SingleCommentEvent();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TargetUserId, Is.EqualTo(AuthorId)); // 父评论作者
        Assert.That(evt.RootReviewGuid, Is.EqualTo(ParentReviewId));
    }

    [Test]
    public async Task 评论正文超长_预览截断50字()
    {
        var longBody = new string('长', 80);

        await HandleAsync(rootReview: null, body: longBody);

        var evt = SingleCommentEvent();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.CommentPreview.Length, Is.EqualTo(50));
    }

    [Test]
    public async Task 图片评论_正文为空_预览为空串()
    {
        // 纯图片评论（body null + 图片合法格式）：CreateDefault 推断为 Image，正文预览为空串
        var images = new List<VideoImage>
        {
            VideoImage.FromUrl(new Uri("https://img.example/a.jpg"))
        };

        var ok = await _handler.Handler(
            new AddVideoReviewCommand(Guid.CreateVersion7(), VideoId, ActorId, null, null, images), default);

        Assert.That(ok, Is.True);
        var evt = SingleCommentEvent();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.CommentPreview, Is.EqualTo(""));
    }

    [Test]
    public async Task 作者评论自己的视频_事件仍发布_自互动由消费端跳过()
    {
        // 自互动过滤发生在 Message 消费端（ActorUserId == TargetUserId 跳过），发布侧不预过滤（保持链路一致）
        await HandleAsync(rootReview: null, body: "自己评论", actor: AuthorId);

        var evt = SingleCommentEvent();
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.ActorUserId, Is.EqualTo(evt.TargetUserId));
    }
}