using System.Linq.Expressions;
using DomainCommons;
using Microsoft.EntityFrameworkCore;

namespace DomainInfrastructure;

/// <summary>
/// EF Core 扩展方法
/// </summary>
public static class EFCoreExtension
{
    /// <summary>
    /// 为所有实现 ISoftDelete 的实体启用软删除全局查询过滤器
    /// </summary>
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
    /// 获取无跟踪的 DbSet 查询
    /// </summary>
    public static IQueryable<T> Query<T>(this DbContext ctx) where T : class
    {
        return ctx.Set<T>().AsNoTracking();
    }
}