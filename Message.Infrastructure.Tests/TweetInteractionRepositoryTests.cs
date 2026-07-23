using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotMediator;

namespace Message.Infrastructure.Tests;

/// <summary>
///   验证 TweetInteractionRepository 修复后 _dbSet 正常工作
/// </summary>
public class TweetInteractionRepositoryTests : IAsyncLifetime
{
    private readonly Message.Infrastructure.Repository.TweetInteractionRepository _repository;
    private readonly MessageDbContext _context;

    public TweetInteractionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MessageDbContext>()
            .UseInMemoryDatabase($"InteractionTestDb_{Guid.NewGuid()}")
            .Options;

        var notMediatorMock = new Mock<INotMediator>();
        _context = new MessageDbContext(options, notMediatorMock.Object);
        _repository = new Message.Infrastructure.Repository.TweetInteractionRepository(_context);
    }

    public async Task InitializeAsync()
    {
        var tweetGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();

        var like = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Like);
        var fav = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Favorite);

        _context.TweetInteractions.AddRange(like, fav);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_Should_Persist_Interaction()
    {
        var tweetGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Coin);

        var result = await _repository.AddAsync(interaction);

        Assert.NotNull(result);
        Assert.Equal(tweetGuid, result.TweetGuid);
        Assert.Equal(userGuid, result.UserGuid);
        Assert.Equal(InteractionType.Coin, result.Type);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Interaction()
    {
        // Add a specific one to find
        var tweetGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Like);
        await _repository.AddAsync(interaction);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAsync(tweetGuid, userGuid, InteractionType.Like);

        Assert.NotNull(result);
        Assert.Equal(tweetGuid, result!.TweetGuid);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Exists()
    {
        var tweetGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        var interaction = TweetInteraction.Create(tweetGuid, userGuid, InteractionType.Share);
        await _repository.AddAsync(interaction);
        await _context.SaveChangesAsync();

        var exists = await _repository.ExistsAsync(tweetGuid, userGuid, InteractionType.Share);

        Assert.True(exists);
    }

    [Fact]
    public async Task GetCountByTweetAsync_Should_Return_Correct_Count()
    {
        var tweetGuid = Guid.NewGuid();
        var interaction1 = TweetInteraction.Create(tweetGuid, Guid.NewGuid(), InteractionType.Like);
        var interaction2 = TweetInteraction.Create(tweetGuid, Guid.NewGuid(), InteractionType.Like);
        await _repository.AddAsync(interaction1);
        await _repository.AddAsync(interaction2);
        await _context.SaveChangesAsync();

        var count = await _repository.GetCountByTweetAsync(tweetGuid, InteractionType.Like);

        Assert.Equal(2, count);
    }
}
