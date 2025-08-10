
using System.Text;

using Identity.Domain.INotDateTime;

namespace Identity.Web.API.Application.DomainEventHandler
{
    public class CreatedByUserDomainEventHandler : INotificationHandler<CreatedByUserDomainEvent>
    {

        private readonly INotDateTime _notDateTime;
        private readonly ILogger<CreatedByUserDomainEventHandler> _logger;
        private readonly INotMemoryCache _notMemoryCache;


        public CreatedByUserDomainEventHandler(INotDateTime notDateTime, ILogger<CreatedByUserDomainEventHandler> logger, INotMemoryCache notMemoryCache)
        {
            _notDateTime = notDateTime;
            _logger = logger;
            _notMemoryCache = notMemoryCache;
        }



        public async Task Handler(CreatedByUserDomainEvent notifications, CancellationToken cancellationToken = default)
        {
            if (notifications is null)
            {
                _logger.LogWarning($"[w(ﾟДﾟ)w{_notDateTime.UtcNow}] 数据为空{nameof(notifications)}");
                return;
            }
            string account = string.Empty;
            if (string.IsNullOrEmpty(notifications.Email))
                account = notifications.PhoneNumber!.PhoneCode;
            else
                account = notifications.Email;
            var generaCode = await GenerateHelper.CreateRandomValueTask(9);
            await _notMemoryCache.AddByMemoryCacheAsync($"Signe_{notifications.Email}", Encoding.UTF8.GetBytes(generaCode));
            _logger.LogInformation($"[（*＾-＾*）{DateTimeOffset.UtcNow} ]成功生成了{account}激活码！");
            return;
        }
    }
}
