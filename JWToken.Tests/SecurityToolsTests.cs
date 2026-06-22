﻿using Notcomd.Token.JWT;

namespace Notcomd.Token.JWT.Tests;

public class JwtRandomTests
{
    [Fact]
    public async Task CreateRandomValueTask_ShouldReturnSixDigitNumber()
    {
        var value = await JwtRandom.CreateRandomValueTask();

        Assert.True(value >= 100000);
        Assert.True(value <= 999999);
    }

    [Fact]
    public async Task CreateRandomStringValueTask_ShouldReturn9Chars()
    {
        var str = await JwtRandom.CreateRandomStringValueTask();

        Assert.Equal(9, str.Length);
        Assert.True(str.All(char.IsLetterOrDigit));
    }

    [Fact]
    public async Task GenerateSecurityStamp_ShouldReturnBase64()
    {
        var stamp = await JwtRandom.GenerateSecurityStamp();

        Assert.NotNull(stamp);
        // Base64: 16 bytes → 24 chars (with padding)
        Assert.True(stamp.Length >= 22);
    }

    [Fact]
    public void GenerateRandomString_CustomLength_ShouldWork()
    {
        var str = JwtRandom.GenerateRandomString(32);

        Assert.Equal(32, str.Length);
        Assert.True(str.All(char.IsLetterOrDigit));
    }

    [Fact]
    public void GenerateRandomString_NumericOnly_ShouldWork()
    {
        var str = JwtRandom.GenerateRandomString(8, "0123456789");

        Assert.Equal(8, str.Length);
        Assert.True(str.All(char.IsDigit));
    }

    [Fact]
    public async Task CreateRandomValueTask_MultipleCalls_ShouldReturnDifferentValues()
    {
        var values = new HashSet<long>();
        for (var i = 0; i < 10; i++)
        {
            values.Add(await JwtRandom.CreateRandomValueTask());
        }

        Assert.True(values.Count >= 8); // 容忍少量冲突
    }
}

public class NvidGeneratorTests
{
    [Fact]
    public void GenerateNvStyleId_ShouldStartWithPrefix()
    {
        var id = NvidGenerator.GenerateNvStyleId(10, "TEST");

        Assert.StartsWith("TEST", id);
        Assert.Equal(14, id.Length); // prefix(4) + random(10)
    }

    [Fact]
    public void GenerateNvStyleIdWithUuid_Length_ShouldStartWithBV()
    {
        var id = NvidGenerator.GenerateNvStyleIdWithUuid(8);

        Assert.StartsWith("BV", id);
        Assert.Equal(10, id.Length); // BV(2) + random(8)
    }

    [Fact]
    public void GenerateNvStyleIdWithUuid_ShouldStartWithNV()
    {
        var id = NvidGenerator.GenerateNvStyleIdWithUuid();

        Assert.StartsWith("NV", id);
        Assert.Equal(12, id.Length); // NV(2) + base62(10)
    }

    [Fact]
    public void GenerateNvStyleId_ShouldUseValidBase62Chars()
    {
        const string validChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var id = NvidGenerator.GenerateNvStyleId(20, "ID");

        foreach (var c in id[2..]) // skip "ID"
            Assert.Contains(c, validChars);
    }

    [Fact]
    public void NewGuidV7_ShouldReturnValidGuid()
    {
        var guid = NvidGenerator.NewGuidV7();

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(7, guid.Version);
    }

    [Fact]
    public void NewGuidString_ShouldReturn32HexChars()
    {
        var str = NvidGenerator.NewGuidString();

        Assert.Equal(32, str.Length);
        Assert.True(str.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')));
    }
}

public class HashHelperTests
{
    [Fact]
    public void ComputeHash_DefaultAlgorithm_ShouldReturnSHA256()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("hello world");
        var hash = HashHelper.ComputeHash(data);

        Assert.Equal(64, hash.Length); // SHA256 = 64 hex chars
        Assert.True(hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void ComputeHash_SameData_ShouldReturnSameHash()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("consistent");

        var hash1 = HashHelper.ComputeHash(data);
        var hash2 = HashHelper.ComputeHash(data);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentData_ShouldReturnDifferentHashes()
    {
        var data1 = System.Text.Encoding.UTF8.GetBytes("data-A");
        var data2 = System.Text.Encoding.UTF8.GetBytes("data-B");

        var hash1 = HashHelper.ComputeHash(data1);
        var hash2 = HashHelper.ComputeHash(data2);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_EmptyContent_ShouldReturnEmpty()
    {
        var result = HashHelper.ComputeHash(Array.Empty<byte>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ComputeHash_NullContent_ShouldReturnEmpty()
    {
        var result = HashHelper.ComputeHash(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void VerifyHash_Matching_ShouldReturnTrue()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("verify-me");
        var hash = HashHelper.ComputeHash(data);

        Assert.True(HashHelper.VerifyHash(data, hash));
    }

    [Fact]
    public void VerifyHash_NonMatching_ShouldReturnFalse()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("original");
        var hash = HashHelper.ComputeHash(data);

        // 改内容
        Assert.False(HashHelper.VerifyHash(
            System.Text.Encoding.UTF8.GetBytes("modified"), hash));
    }

    [Fact]
    public void ComputeHash_MD5_ShouldReturn32Chars()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("test md5");
        var hash = HashHelper.ComputeHash(data, "MD5");

        Assert.Equal(32, hash.Length);
    }
}
