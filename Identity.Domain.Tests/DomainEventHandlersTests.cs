using System.Runtime.CompilerServices;
using Identity.Domain.Entities.UserAggregate;
using Identity.Domain.Events;
using Identity.Domain.IRepository;
using Identity.Domain.SeedWork;
using Identity.Web.API.Application.DomainEventHandlers;
using Microsoft.Extensions.Logging;
using Moq;
using Notcomd.NotEmail.Core;

namespace Identity.Domain.Tests;

public class AccountLockedEventHandlerTests
{
    private readonly Mock<ILogger<AccountLockedEventHandler>> _loggerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly AccountLockedEventHandler _handler;

    public AccountLockedEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<AccountLockedEventHandler>>();
        _emailSenderMock = new Mock<IEmailSender>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new AccountLockedEventHandler(
            _loggerMock.Object, _emailSenderMock.Object, _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handler_UserExists_ShouldSendEmailAndLog()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var userEmail = "test@notblog.com";
        var notification = new AccountLockedEvent(userGuid);
        var user = CreateUser(userGuid, userEmail);

        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);
        _emailSenderMock
            .Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok());

        // Act
        await _handler.Handler(notification, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(
            e => e.SendAsync(
                It.Is<EmailMessage>(m => m.To == userEmail && m.Subject.Contains("账户锁定")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_UserNotFound_ShouldNotSendEmail()
    {
        var userGuid = Guid.NewGuid();
        var notification = new AccountLockedEvent(userGuid);

        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync((User?)null);

        await _handler.Handler(notification, CancellationToken.None);

        _emailSenderMock.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handler_UserEmailIsNull_ShouldNotSendEmail()
    {
        var userGuid = Guid.NewGuid();
        var notification = new AccountLockedEvent(userGuid);
        var user = CreateUser(userGuid, email: null);

        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);

        await _handler.Handler(notification, CancellationToken.None);

        _emailSenderMock.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handler_EmailSendFails_ShouldNotThrow()
    {
        var userGuid = Guid.NewGuid();
        var userEmail = "test@notblog.com";
        var notification = new AccountLockedEvent(userGuid);
        var user = CreateUser(userGuid, userEmail);

        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);
        _emailSenderMock
            .Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Fail("SMTP error"));

        await _handler.Handler(notification, CancellationToken.None);

        _emailSenderMock.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_RepositoryThrows_ShouldNotThrow()
    {
        var userGuid = Guid.NewGuid();
        var notification = new AccountLockedEvent(userGuid);

        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        await _handler.Handler(notification, CancellationToken.None);

        _emailSenderMock.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建用于测试的 User 实例，绕过构造函数以避免 UserSafety 验证问题
    /// </summary>
    private static User CreateUser(Guid userGuid, string? email)
    {
        var user = (User)RuntimeHelpers.GetUninitializedObject(typeof(User));
        typeof(User).GetProperty(nameof(User.UserGuid))!.SetValue(user, userGuid);
        typeof(User).GetProperty(nameof(User.UserEmail))!.SetValue(user, email);
        typeof(User).GetProperty(nameof(User.UserRoleGuid))!.SetValue(user, new HashSet<Guid>());
        typeof(User).GetProperty(nameof(User.AuthorGuids))!.SetValue(user, new HashSet<Guid>());
        return user;
    }
}

public class PhoneNumberBandingEventHandlerTests
{
    [Fact]
    public async Task Handler_ShouldLogNotificationWithoutException()
    {
        var loggerMock = new Mock<ILogger<PhoneNumberBandingEventHandler>>();
        var handler = new PhoneNumberBandingEventHandler(loggerMock.Object);
        var notification = new PhoneNumberBandingEvent(Guid.NewGuid(), "13800138000");

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }
}

public class PhoneNumberChangeEventHandlerTests
{
    [Fact]
    public async Task Handler_ShouldLogChangeWithoutException()
    {
        var loggerMock = new Mock<ILogger<PhoneNumberChangeEventHandler>>();
        var handler = new PhoneNumberChangeEventHandler(loggerMock.Object);
        var notification = new PhoneNumberChangeDomainEvent(Guid.NewGuid(), "13900139000");

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }
}

public class RoleStartedEventHandlerTests
{
    [Fact]
    public async Task Handler_ShouldLogRoleCreationWithoutException()
    {
        var loggerMock = new Mock<ILogger<RoleStartedEventHandler>>();
        var handler = new RoleStartedEventHandler(loggerMock.Object);
        var roles = Identity.Domain.Entities.RoleAggregate.Roles.RoleFactory.CreateUserRole();
        var notification = new RoleStartedDomainEvent(roles);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }
}

public class RoleStatusChangeEventHandlerTests
{
    [Fact]
    public async Task Handler_ShouldLogStatusChangeWithoutException()
    {
        var loggerMock = new Mock<ILogger<RoleStatusChangeEventHandler>>();
        var handler = new RoleStatusChangeEventHandler(loggerMock.Object);
        var notification = new RoleStatusChangeDomainEvent(
            Guid.NewGuid(),
            Identity.Domain.Entities.RoleAggregate.RoleStatus.Disabled);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }
}

public class UserStartedByPhoneEventHandlerTests
{
    private readonly Mock<ILogger<UserStartedByPhoneEventHandler>> _loggerMock;
    private readonly Mock<IUserRoleRepository> _userRoleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserStartedByPhoneEventHandler _handler;

    public UserStartedByPhoneEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UserStartedByPhoneEventHandler>>();
        _userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userRoleRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _userRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);

        _handler = new UserStartedByPhoneEventHandler(
            _loggerMock.Object, _userRoleRepositoryMock.Object, _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handler_ShouldAssignDefaultRoleToUser()
    {
        var userGuid = Guid.NewGuid();
        var phoneNumber = PhoneNumber.CreatePhoneNumber(86, "13800138000");
        var notification = new UserStartedByPhoneDomainEvent(
            userGuid, new HashSet<Guid>(), phoneNumber, null);
        var defaultRole = Identity.Domain.Entities.RoleAggregate.Roles.RoleFactory.CreateUserRole();
        var user = CreateTestUser(userGuid);

        _userRoleRepositoryMock.Setup(r => r.FindByUserRoleAsync("User")).ReturnsAsync(defaultRole);
        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);

        await _handler.Handler(notification, CancellationToken.None);

        _userRepositoryMock.Verify(r => r.UpdateByUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handler_UserNotFound_ShouldNotUpdate()
    {
        var userGuid = Guid.NewGuid();
        var phoneNumber = PhoneNumber.CreatePhoneNumber(86, "13800138000");
        var notification = new UserStartedByPhoneDomainEvent(
            userGuid, new HashSet<Guid>(), phoneNumber, null);
        var defaultRole = Identity.Domain.Entities.RoleAggregate.Roles.RoleFactory.CreateUserRole();

        _userRoleRepositoryMock.Setup(r => r.FindByUserRoleAsync("User")).ReturnsAsync(defaultRole);
        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync((User?)null);

        await _handler.Handler(notification, CancellationToken.None);

        _userRepositoryMock.Verify(r => r.UpdateByUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handler_UserAlreadyHasRole_ShouldNotReassign()
    {
        var userGuid = Guid.NewGuid();
        var phoneNumber = PhoneNumber.CreatePhoneNumber(86, "13800138000");
        var defaultRole = Identity.Domain.Entities.RoleAggregate.Roles.RoleFactory.CreateUserRole();
        var user = CreateTestUser(userGuid);
        user.UserRoleGuid.Add(defaultRole.RoleGuid);
        var notification = new UserStartedByPhoneDomainEvent(
            userGuid, new HashSet<Guid>(), phoneNumber, null);

        _userRoleRepositoryMock.Setup(r => r.FindByUserRoleAsync("User")).ReturnsAsync(defaultRole);
        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);

        await _handler.Handler(notification, CancellationToken.None);

        _userRepositoryMock.Verify(r => r.UpdateByUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handler_DefaultRoleNotExists_ShouldCreateAndAssign()
    {
        var userGuid = Guid.NewGuid();
        var phoneNumber = PhoneNumber.CreatePhoneNumber(86, "13800138000");
        var notification = new UserStartedByPhoneDomainEvent(
            userGuid, new HashSet<Guid>(), phoneNumber, null);
        var user = CreateTestUser(userGuid);

        _userRoleRepositoryMock.Setup(r => r.FindByUserRoleAsync("User")).ThrowsAsync(new ArgumentNullException());
        _userRepositoryMock.Setup(r => r.FindOneByUserAsync(userGuid)).ReturnsAsync(user);

        await _handler.Handler(notification, CancellationToken.None);

        _userRoleRepositoryMock.Verify(
            r => r.AddByUserRoleAsync(It.IsAny<Identity.Domain.Entities.RoleAggregate.Roles>()),
            Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateByUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handler_ExceptionInRepository_ShouldNotThrow()
    {
        var userGuid = Guid.NewGuid();
        var phoneNumber = PhoneNumber.CreatePhoneNumber(86, "13800138000");
        var notification = new UserStartedByPhoneDomainEvent(
            userGuid, new HashSet<Guid>(), phoneNumber, null);

        _userRoleRepositoryMock.Setup(r => r.FindByUserRoleAsync("User"))
            .ThrowsAsync(new InvalidOperationException("DB connection error"));

        var ex = await Record.ExceptionAsync(() => _handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }

    private static User CreateTestUser(Guid userGuid)
    {
        var user = (User)RuntimeHelpers.GetUninitializedObject(typeof(User));
        typeof(User).GetProperty(nameof(User.UserGuid))!.SetValue(user, userGuid);
        typeof(User).GetProperty(nameof(User.UserRoleGuid))!.SetValue(user, new HashSet<Guid>());
        typeof(User).GetProperty(nameof(User.AuthorGuids))!.SetValue(user, new HashSet<Guid>());
        return user;
    }
}

public class UserStatusChangeEventHandlerTests
{
    [Fact]
    public async Task Handler_UserDisabled_ShouldNotThrow()
    {
        var loggerMock = new Mock<ILogger<UserStatusChangeEventHandler>>();
        var handler = new UserStatusChangeEventHandler(loggerMock.Object);
        var notification = new UserStatusChangeDomainEvent(Guid.NewGuid(), UserStatus.Disabled);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Handler_UserNormal_ShouldNotThrow()
    {
        var loggerMock = new Mock<ILogger<UserStatusChangeEventHandler>>();
        var handler = new UserStatusChangeEventHandler(loggerMock.Object);
        var notification = new UserStatusChangeDomainEvent(Guid.NewGuid(), UserStatus.Normal);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Handler_UserLocked_ShouldNotThrow()
    {
        var loggerMock = new Mock<ILogger<UserStatusChangeEventHandler>>();
        var handler = new UserStatusChangeEventHandler(loggerMock.Object);
        var notification = new UserStatusChangeDomainEvent(Guid.NewGuid(), UserStatus.Locked);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Handler_UserDeleted_ShouldNotThrow()
    {
        var loggerMock = new Mock<ILogger<UserStatusChangeEventHandler>>();
        var handler = new UserStatusChangeEventHandler(loggerMock.Object);
        var notification = new UserStatusChangeDomainEvent(Guid.NewGuid(), UserStatus.Deleted);

        var ex = await Record.ExceptionAsync(() => handler.Handler(notification, CancellationToken.None));

        Assert.Null(ex);
    }
}
