namespace Message.Web.API.Application.Commands.Notifications;

/// <summary>全部通知标记已读命令。</summary>
public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<bool>;
