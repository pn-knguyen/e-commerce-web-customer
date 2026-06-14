using System.Linq.Expressions;

namespace e_commerce_web_customer.Interfaces;

public interface IReadRepository<TEntity>
    where TEntity : class
{
    IQueryable<TEntity> AsQueryable();

    Task<List<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(params object[] keyValues);

    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
}
