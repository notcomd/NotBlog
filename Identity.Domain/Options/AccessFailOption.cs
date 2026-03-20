namespace Identity.Domain.Options;

public abstract class AccessFailOption
{
    public int MiximumAccessFailCount { get; set; } = 5;

    public TimeSpan LockOutDuration { get; set; } = TimeSpan.FromMinutes(15);
}