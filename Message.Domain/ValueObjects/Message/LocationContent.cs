
namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 位置消息内容值对象。
/// 不可变，封装经纬度与位置名称，经纬度范围在构造时校验。
/// </summary>
public class LocationContent : MessageContent
{
    private LocationContent(double latitude, double longitude, string locationName)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("纬度必须在-90到90之间");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("经度必须在-180到180之间");

        if (string.IsNullOrWhiteSpace(locationName))
            throw new ArgumentException("位置名称不能为空");

        Latitude = latitude;
        Longitude = longitude;
        LocationName = locationName;
    }

    /// <summary>纬度（-90 到 90）</summary>
    public double Latitude { get; }
    /// <summary>经度（-180 到 180）</summary>
    public double Longitude { get; }
    /// <summary>位置名称（非空）</summary>
    public string LocationName { get; }

    /// <summary>内容业务类型，恒为位置消息。</summary>
    public override MessageType MessageType => MessageType.MessageLocation;

    /// <summary>创建位置内容值对象（校验经纬度范围与名称非空）。</summary>
    public static LocationContent Create(double latitude, double longitude, string locationName)
        => new(latitude, longitude, locationName);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Latitude;
        yield return Longitude;
        yield return LocationName;
    }

    /// <summary>生成会话侧栏摘要，形如「[位置] 名称」。</summary>
    public override string ToSessionSummary() => $"[位置] {LocationName}";
}
