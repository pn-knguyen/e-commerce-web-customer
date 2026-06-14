using System.Linq.Expressions;
using e_commerce_web_customer.Data;
using e_commerce_web_customer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_customer.Repositories;

public class ReadRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class
{
    private readonly EcommerceDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public ReadRepository(EcommerceDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> AsQueryable()
    {
        return _dbSet.AsNoTracking().AsQueryable();
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(params object[] keyValues)
    {
        return await _dbSet.FindAsync(keyValues);
    }

    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }
}
