namespace FileDev.Web.API.Extensions;
using System.Diagnostics;
internal static class ActivityExtensions
{

    public static void SetExceptionTags(this Activity activity,Exception exception)
    {
        if (exception is null)
        {
            return;
        }
        activity.AddTag("Exception", exception.Message);
        activity.AddTag("ExceptionType", exception.GetType().Name);
        activity.AddTag("ExceptionStack", exception.StackTrace);
        activity.AddTag("ExceptionInner", exception.InnerException?.Message);
        activity.AddTag("ExceptionInnerType", exception.InnerException?.GetType().Name);
        activity.SetStatus(ActivityStatusCode.Error);
            
    }
}
