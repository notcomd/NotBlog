namespace Notcomd.Token.JWT;

public static class JwtRandom
{
    public static ValueTask<long> CreateRandomValueTask()
    {
        var random = new Random().NextInt64(100000,999999);
        return new ValueTask<long>(random);
    }
}