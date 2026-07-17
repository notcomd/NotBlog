using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.IRepository;
using Microsoft.Extensions.Logging;
using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class TweetInteractionEventHandler(
    ITweetRepository tweetRepository,
    ILogger<TweetInteractionEventHandler> logger) : INotificationHandler<TweetInteractionEvent>
{
    private readonly ITweetRepository _tweetRepository = tweetRepository;
    private readonly ILogger<TweetInteractionEventHandler> _logger = logger;

    public async Task Handler(TweetInteractionEvent notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "处理推文互动事件: TweetGuid={TweetGuid}, UserGuid={UserGuid}, InteractionType={InteractionType}, IsAdd={IsAdd}",
                notification.TweetGuid, notification.UserGuid, notification.InteractionType, notification.IsAdd);

            _logger.LogInformation("[{Time}] 推文互动: TweetGuid={TweetGuid}, UserGuid={UserGuid}, Type={InteractionType}, IsAdd={IsAdd}",
                DateTimeOffset.UtcNow, notification.TweetGuid, notification.UserGuid, notification.InteractionType, notification.IsAdd);

            var tweet = await _tweetRepository.GetByIdAsync(notification.TweetGuid);
            if (tweet is null)
            {
                _logger.LogWarning("推文互动事件处理时推文不存在: TweetGuid={TweetGuid}", notification.TweetGuid);
                return;
            }

            tweet.RecalculateHotScore();
            await _tweetRepository.UpdateAsync(tweet);
            await _tweetRepository.UnitOfWork.SavaChangesAsync(cancellationToken);

            _logger.LogInformation("推文热度已重新计算: TweetGuid={TweetGuid}, HotScore={HotScore}",
                notification.TweetGuid, tweet.HotScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理推文互动事件失败: TweetGuid={TweetGuid}", notification.TweetGuid);
        }
    }
}
