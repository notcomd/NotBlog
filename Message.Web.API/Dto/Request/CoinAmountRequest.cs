namespace Message.Web.API.Dto.Request;

/// <summary>硬币变动请求（增加/扣除共用）。</summary>
public class CoinAmountRequest
{
    public long Amount { get; init; }
}
