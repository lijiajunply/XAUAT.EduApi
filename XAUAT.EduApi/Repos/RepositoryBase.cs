using System.Linq.Expressions;
using EduApi.Data;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public abstract class RepositoryBase<T>(IDbContextFactory<EduContext> contextFactory) : IRepository<T>
    where T : class
{
    protected EduContext CreateContext() => contextFactory.CreateDbContext();

    public async Task<T?> GetByIdAsync(string id)
    {
        await using var context = CreateContext();
        return await context.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        await using var context = CreateContext();
        return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await using var context = CreateContext();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        await using var context = CreateContext();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        await using var context = CreateContext();
        context.Set<T>().Remove(entity);
        await context.SaveChangesAsync();
    }
}