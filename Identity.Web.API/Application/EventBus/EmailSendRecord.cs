namespace Identity.Web.API.Application.EventBus;

public record EmailSendRecord([EmailAddress(ErrorMessage = "格式错误")]string ToEmail, long Code);