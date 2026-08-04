using Commons.SeedWork;

namespace Message.Domain.ValueObjects.Message;

/// <summary>
/// 位置消息内容值对象。
/// 不可变，封装经纬度与位置名称，经纬度范围在构造时校验。
/// </summary>
public class LocationContent : ValueObject
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

    public double Latitude { get; }
    public double Longitude { get; }
    public string LocationName { get; }

    public static LocationContent Create(double latitude, double longitude, string locationName)
        => new(latitude, longitude, locationName);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Latitude;
        yield return Longitude;
        yield return LocationName;
    }
}
