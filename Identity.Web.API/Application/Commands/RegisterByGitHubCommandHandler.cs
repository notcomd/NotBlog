using System.Security.Claims;
using System.Security.Cryptography;
using CacheMemory.Core;
using Identity.Domain.Dto.OAuth;
using Identity.Domain.Entities.RoleAggregate;
using Identity.Domain.Entities.UserExternalLoginAggregate;
using Identity.Web.API.Application.IntegrationEvents.Events;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.Application.Commands;

public class RegisterByGitHubCommandHandler(
    IGitHubAuthService gitHubAuthService,
    IUserExternalLoginRepository userExternalLoginRepository,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    IJwtTokenService jwtTokenService,
    IOptionsSnapshot<JwtOptions> jwtOptions,
    ICacheMemory<TokenCacheEntry> cacheMemory,
    IEventBus eventBus,
    ILogger<RegisterByGitHubCommandHandler> logger)
    : NotMediator.IRequestHandler<RegisterByGitHubCommand, RegisterByGitHubResult>
{
    private const string AccessTokenKeyPrefix = "auth:token";
    private const string RefreshTokenKeyPrefix = "auth:refresh";

    public async Task<RegisterByGitHubResult> Handler(
        RegisterByGitHubCommand command, CancellationToken cancellationToken)
    {
        var githubUser = await gitHubAuthService.GetGitHubUserAsync(command.Code);
        if (githubUser is null)
            throw new InvalidOperationException("无法获取 GitHub 用户信息，请重新授权");

        logger.LogInformation(
            "[RegisterByGitHub] GitHub 用户: Id={Id}, Login={Login}, Email={Email}",
            githubUser.Id, githubUser.Login, githubUser.Email);

        var existingLogin = await userExternalLoginRepository
            .FindOneByUserIdAndProviderAsync(LoginProviderType.GitHub, githubUser.Id);

        User user;
        UserExternalLogin externalLogin;
        bool isNewUser;

        if (existingLogin is not null)
        {
            externalLogin = existingLogin;
            isNewUser = false;

            if (externalLogin.UserId != Guid.Empty)
            {
                user = (await userRepository.FindOneByUserAsync(externalLogin.UserId))
                    ?? throw new InvalidOperationException("关联的用户账号不存在");
                logger.LogInformation("[RegisterByGitHub] 已有绑定，直接登录: UserId={UserId}", user.UserGuid);
            }
            else
            {
                user = await FindOrCreateUserAsync(githubUser);
                isNewUser = true;
                externalLogin.UpdateTokens(githubUser.AccessToken, null, null);
                LinkExternalLoginToUser(externalLogin, user);
                await userExternalLoginRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
            }
        }
        else
        {
            isNewUser = true;
            user = await FindOrCreateUserAsync(githubUser);

            externalLogin = UserExternalLogin.Create(
                LoginProviderType.GitHub, githubUser.Id, githubUser.Name);
            externalLogin.UpdateTokens(githubUser.AccessToken, null, null);
            LinkExternalLoginToUser(externalLogin, user);

            await userExternalLoginRepository.AddAsync(externalLogin);
            await userExternalLoginRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);
        }

        if (isNewUser)
        {
            await eventBus.PublishAsync(new RegisterByUserIntegrationEvent(user.UserGuid));
            logger.LogInformation(
                "[RegisterByGitHub] 新用户注册: UserId={UserId}, GitHubId={GitHubId}",
                user.UserGuid, githubUser.Id);
        }

        var token = await GenerateTokenAsync(user);

        return new RegisterByGitHubResult(
            isNewUser, token,
            user.UserGuid.ToString(),
            user.UserName ?? githubUser.Login,
            user.UserEmail,
            githubUser.AvatarUrl);
    }

    private async Task<User> FindOrCreateUserAsync(GitHubUserInfo githubUser)
    {
        if (!string.IsNullOrEmpty(githubUser.Email))
        {
            var existingUser = await userRepository.FindOneByUserAsync(githubUser.Email);
            if (existingUser is not null)
            {
                logger.LogInformation("[RegisterByGitHub] 邮箱匹配已有用户: Email={Email}", githubUser.Email);
                return existingUser;
            }
        }
        return await CreateUserFromGitHubAsync(githubUser);
    }

    private async Task<User> CreateUserFromGitHubAsync(GitHubUserInfo githubUser)
    {
        var userRole = await userRoleRepository.FindByUserRoleAsync("User");
        if (userRole is null)
        {
            userRole = Roles.RoleFactory.CreateUserRole();
            await userRoleRepository.AddByUserRoleAsync(userRole);
            await userRoleRepository.UnitOfWork.SavaEntitiesAsync();
        }

        var email = !string.IsNullOrEmpty(githubUser.Email)
            ? githubUser.Email
            : $"{githubUser.Login}@github.user";

        var user = await User.CreateByEmailUser(
            userRole.RoleGuid, email, GenerateRandomPassword(), null, null);

        await userRepository.AddOneByUserAsync(user);
        await userRepository.UnitOfWork.SavaChangesAsync();

        logger.LogInformation("[RegisterByGitHub] 创建新用户: UserId={UserId}, Email={Email}",
            user.UserGuid, email);
        return user;
    }

    private static void LinkExternalLoginToUser(UserExternalLogin externalLogin, User user)
    {
        externalLogin.LinkUser(user.UserGuid);
        user.LinkAuthority(externalLogin.LoginId);
    }

    private async Task<TokenResult> GenerateTokenAsync(User user)
    {
        var roleNames = new List<string>();
        foreach (var roleGuid in user.UserRoleGuid)
        {
            var role = await userRoleRepository.FindByUserRoleAsync(roleGuid);
            if (role is not null) roleNames.Add(role.RoleName);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserGuid.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.UserEmail),
            new(ClaimTypes.Email, user.UserEmail),
            new(ClaimTypes.Role, string.Join(",", roleNames)),
            new("user_guid", user.UserGuid.ToString()),
            new("login_provider", "github")
        };

        var config = jwtOptions.Value;
        var tokenResult = await jwtTokenService.BuildTokenAsync(claims, config);

        var accessTtl = config.ExpireSeconds > 0
            ? TimeSpan.FromSeconds(config.ExpireSeconds) : TimeSpan.FromHours(1);
        await cacheMemory.SetAsync(
            $"{AccessTokenKeyPrefix}:{user.UserGuid}",
            new TokenCacheEntry
            {
                Token = tokenResult.AccessToken,
                UserGuid = user.UserGuid,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = tokenResult.ExpiresAt,
                TokenType = tokenResult.TokenType,
                LinkedAccessToken = tokenResult.RefreshToken
            }, accessTtl);

        if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
        {
            await cacheMemory.SetAsync(
                $"{RefreshTokenKeyPrefix}:{user.UserGuid}",
                new TokenCacheEntry
                {
                    Token = tokenResult.RefreshToken,
                    UserGuid = user.UserGuid,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(config.RefreshTokenExpireSeconds),
                    TokenType = "refresh",
                    LinkedAccessToken = tokenResult.AccessToken
                },
                TimeSpan.FromSeconds(config.RefreshTokenExpireSeconds > 0
                    ? config.RefreshTokenExpireSeconds : 604800));
        }

        return tokenResult;
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=<>?";
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(32, bytes, (span, state) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = chars[state[i] % chars.Length];
        });
    }
}
