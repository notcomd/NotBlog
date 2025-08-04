namespace Identity.Domain.AggregatesModel.ClientAggregate
{
    public class NotClient : Entity, IAggregateRoot
    {


        protected NotClient()
        {

        } // EF Core needs a parameterless constructor

        public NotClient(Guid userGuid,string notClientName, string notClientDescription, string notClientPrivateKey, string notClientSecret, string notClientUri, string notClientType)
        {
            UserGuid = userGuid != Guid.Empty ? userGuid : throw new ArgumentNullException(nameof(userGuid), "UserGuid cannot be empty");
            NotClientName = notClientName ?? throw new ArgumentNullException(nameof(notClientName));
            NotClientDescription = notClientDescription ?? throw new ArgumentNullException(nameof(notClientDescription));
            NotClientPrivateKey = notClientPrivateKey ?? throw new ArgumentNullException(nameof(notClientPrivateKey));
            NotClientSecret = notClientSecret ?? throw new ArgumentNullException(nameof(notClientSecret));
            NotClientUri = notClientUri ?? throw new ArgumentNullException(nameof(notClientUri));
            NotClientType = notClientType ?? throw new ArgumentNullException(nameof(notClientType));
        }

        /// <summary>
        ///  客户端的唯一标识符
        /// </summary>
        public Guid NotClientGuid { get; init; } = Guid.CreateVersion7();

        /// <summary>
        /// 授权用户的唯一标识符
        /// </summary>
        public Guid UserGuid { get; private set; }=Guid.Empty;

        /// <summary>
        /// 客户端名称
        /// </summary>
        public string NotClientName { get; private set; } = string.Empty;

        /// <summary>
        ///  客户端描述
        /// </summary>
        public string NotClientDescription { get; private set; } = string.Empty;

        /// <summary>
        /// 客户端密钥
        /// </summary>
        public string NotClientPrivateKey { get; private set; } = null!;

        /// <summary>
        ///  客户端密言
        /// </summary>
        public string NotClientSecret { get; private set; } = null!;

        /// <summary>
        ///  客户端Uri
        /// </summary>
        public string NotClientUri { get; private set; } = string.Empty;

        /// <summary>
        /// 客户端类型
        /// </summary>
        public string NotClientType { get; private set; } = string.Empty;


        private void AddNotClientStartDomainEventBus(string clientName, string clientDescription, string clientPrivateKey
            , string clientSecret, string clienturi)
        {

        }
    }
}