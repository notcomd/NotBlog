namespace Identity.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    private Address()
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

    public string Country { get; init; } = null!; // 国家

    public string Province { get; init; } = null!; // 省份

    public string City { get; init; } = null!; // 城市

    public string District { get; init; } = null!; // 区县

    public string Street { get; init; } = null!; // 街道

    public string Detail { get; init; } = null!; // 详细地址


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
