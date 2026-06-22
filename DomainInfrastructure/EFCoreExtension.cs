using System.Linq.Expressions;
using DomainCommons;
using Microsoft.EntityFrameworkCore;

namespace DomainInfrastructure;

/// <summary>
/// EF Core 扩展方法
/// 提供软删除全局过滤器、查询辅助等通用 EF Core 扩展。
/// </summary>
public static class EFCoreExtension
{
    /// <summary>
    /// 为所有实现了 <see cref="ISoftDelete"/> 的实体类型启用软删除全局查询过滤器。
    /// 在 OnModelCreating 中调用此方法后，所有查询将自动过滤掉 IsDeleted=true 的记录。
    /// </summary>
    /// <param name="modelBuilder">ModelBuilder 实例</param>
    public static void EnableSoftDeletionGlobalFilter(this ModelBuilder modelBuilder)
    {
        var entityTypesWithSoftDeletion = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.ClrType.IsAssignableTo(typeof(ISoftDelete)));

        foreach (var entityType in entityTypesWithSoftDeletion)
        {
            var isDeletedProperty = entityType.FindProperty(nameof(ISoftDelete.IsDeleted));
            if (isDeletedProperty == null) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "p");
            var filter = Expression.Lambda(
                Expression.Not(Expression.Property(parameter, isDeletedProperty.PropertyInfo)),
                parameter);
            entityType.SetQueryFilter(filter);
        }
    }

    /// <summary>
    /// 获取无跟踪（AsNoTracking）的 DbSet 查询。
    /// 查询返回的实体不会被 ChangeTracker 追踪，适用于只读场景。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="ctx">DbContext 实例</param>
    /// <returns>无跟踪的 IQueryable 查询</returns>
    public static IQueryable<T> Query<T>(this DbContext ctx) where T : class
    {
        return ctx.Set<T>().AsNoTracking();
    }
}