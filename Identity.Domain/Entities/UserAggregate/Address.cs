namespace Identity.Domain.Entities.UserAggregate;

public sealed class Address : ValueObject
{
    protected Address()
    {
    }


    public Address(string country, string province, string city, string district, string street, string detail)
    {
        Country = country;
        Province = province;
        City = city;
        District = district;
        Street = street;
        Detail = detail;
    }

    public string Country { get; init; } // 国家

    public string Province { get; init; } // 省份

    public string City { get; init; } // 城市

    public string District { get; init; } // 区县

    public string Street { get; init; } // 街道

    public string Detail { get; init; } // 详细地址


    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Country;
        yield return Province;
        yield return City;
        yield return District;
        yield return Street;
        yield return Detail;
    }
}