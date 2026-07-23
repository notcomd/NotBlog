using System.ComponentModel.DataAnnotations;
using Notcomd.EventBus.Core;

namespace Identity.Web.API.Application.IntegrationEvents;

public record EmailSendRecord([EmailAddress(ErrorMessage = "格式错误")] string ToEmail, long Code) : IntegrationEvent;
