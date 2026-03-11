using Markdown.Domain.Entities;

namespace Markdown.Domain.IRepository;

public interface IMarkDownDbContextRepository<TEntityDbContext> where TEntityDbContext : IAggregateRoot
{
    /// <summary>
    ///     添加内内容
    /// </summary>
    /// <returns>void</returns>
    Task AddMarkDownAsync();

    /// <summary>
    ///     查找内容
    /// </summary>
    /// <param name="entityDbcontext"></param>
    /// <returns>放回TentityDbContext对象</returns>
    Task<TEntityDbContext> FindAsync(TEntityDbContext entityDbcontext);

    /// <summary>
    ///     更新markdown内容
    /// </summary>
    /// <param name="entityDbcontext"></param>
    /// <returns>void</returns>
    Task UpDataAsync(TEntityDbContext entityDbcontext);

    /// <summary>
    /// </summary>
    /// <param name="entityDbContext"></param>
    /// <returns></returns>
    Task DeleteAsync(TEntityDbContext entityDbContext);
}