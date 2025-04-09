using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Identity.Web.API.EventBus;

public record EmailSendRecord([EmailAddress(ErrorMessage = "格式错误")]string ToEmail, long Code) : INotification;