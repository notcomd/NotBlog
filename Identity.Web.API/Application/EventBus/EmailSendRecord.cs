namespace Identity.Web.API.Application.EventBus;

public record EmailSendRecord(string Subject,[EmailAddress(ErrorMessage = "格式错误")]string ToEmail, string Code);