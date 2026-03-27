namespace DomainInfrastructure;

/// <summary>
/// 标记需要自动保存的 DbContext
/// 用于 UnitOfWorkFilter 在 Action 执行后自动调用 SaveChangesAsync
/// 
/// 使用示例：
///   [SaverDbContext(typeof(MessageDbContext))]
///   [SaverDbContext(typeof(MessageDbContext), typeof(IdentityDbContext))]
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SaverDbContextAttribute : Attribute
{
    public SaverDbContextAttribute(params Type[] dbContextTypes)
    {
        DbContextTypes = dbContextTypes ?? Array.Empty<Type>();
    }

    public Type[] DbContextTypes { get; }
}