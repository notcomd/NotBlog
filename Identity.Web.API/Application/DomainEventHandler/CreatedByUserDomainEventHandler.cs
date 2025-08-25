using Identity.Web.API.Application.Command;

namespace Identity.Web.API.Application.DomainEventHandler
{
    public class CreatedByUserDomainEventHandler : INotificationHandler<CreatedByUserDomainEvent>
    {

        private readonly INotDateTime _notDateTime;
        private readonly ILogger<CreatedByUserDomainEventHandler> _logger;
        private readonly INotMediator _notMediator;



        public CreatedByUserDomainEventHandler(INotDateTime notDateTime, ILogger<CreatedByUserDomainEventHandler> logger, INotMediator notMediator)
        {
            _notDateTime = notDateTime;
            _logger = logger;
            _notMediator = notMediator;

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
            var changeCode = new GenerateCodeCommand(account, 9);
            await _notMediator.SendAsync(changeCode, cancellationToken);
            _logger.LogInformation($"[（*＾-＾*）{_notDateTime.UtcNow} ]成功生成了{account}激活码！");
            return;
        }


    }
}
