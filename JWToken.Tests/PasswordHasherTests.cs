using System.Security.Cryptography;
using Notcomd.Token.JWT;

namespace Notcomd.Token.JWT.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnHashAndSalt()
    {
        var (hash, salt) = PasswordHasher.Hash("MySecurePassword123!");

        Assert.NotNull(hash);
        Assert.NotNull(salt);
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var (hash, salt) = PasswordHasher.Hash("correct-password");

        var result = PasswordHasher.Verify("correct-password", hash, salt);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var (hash, salt) = PasswordHasher.Hash("right-password");

        var result = PasswordHasher.Verify("wrong-password", hash, salt);

        Assert.False(result);
    }

    [Fact]
    public void Hash_DifferentPasswords_ShouldProduceDifferentHashes()
    {
        var (hash1, _) = PasswordHasher.Hash("password-A");
        var (hash2, _) = PasswordHasher.Hash("password-B");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_SamePassword_ShouldProduceDifferentResults()
    {
        var (hash1, _) = PasswordHasher.Hash("same-password");
        var (hash2, _) = PasswordHasher.Hash("same-password");

        // 不同 salt，所以 hash 不同
        Assert.NotEqual(hash1, hash2);
    }
}

public class HashH256ToolTests
{
    [Fact]
    public async Task CreateHash256Async_ShouldReturnHash()
    {
        var salt = await HashH256Tool.GenerateSValueTask();

        var hash = await HashH256Tool.CreateHash256Async("test-password", salt);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public async Task GenerateSValueTask_ShouldReturn64Bytes()
    {
        var salt = await HashH256Tool.GenerateSValueTask();

        Assert.Equal(64, salt.Length);
    }

    [Fact]
    public async Task VerifyPasswordValueTask_CorrectPassword_ShouldReturnTrue()
    {
        var salt = await HashH256Tool.GenerateSValueTask();
        var hash = await HashH256Tool.CreateHash256Async("mypassword", salt);

        var result = await HashH256Tool.VerifyPasswordValueTask("mypassword", hash, salt);

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyPasswordValueTask_WrongPassword_ShouldReturnFalse()
    {
        var salt = await HashH256Tool.GenerateSValueTask();
        var hash = await HashH256Tool.CreateHash256Async("correct", salt);

        var result = await HashH256Tool.VerifyPasswordValueTask("incorrect", hash, salt);

        Assert.False(result);
    }
}
