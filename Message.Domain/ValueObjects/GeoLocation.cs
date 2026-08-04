using Commons.SeedWork;

namespace Message.Domain.ValueObjects;

public class GeoLocation : ValueObject
{
    public GeoLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("纬度必须在 -90 到 90 之间", nameof(latitude));

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("经度必须在 -180 到 180 之间", nameof(longitude));

        Latitude = Math.Round(latitude, 6);
        Longitude = Math.Round(longitude, 6);
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public double DistanceTo(GeoLocation other)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var lat1 = ToRadians(Latitude);
        var lat2 = ToRadians(other.Latitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    public bool IsNear(GeoLocation other, double radiusKm) => DistanceTo(other) <= radiusKm;

    public string ToGoogleMapsUrl() => $"https://www.google.com/maps?q={Latitude},{Longitude}";
    public string ToBaiduMapsUrl() => $"https://api.map.baidu.com/marker?location={Latitude},{Longitude}";

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override string ToString() => $"{Latitude:F6},{Longitude:F6}";
}