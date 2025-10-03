using System.Linq.Expressions;
using EduApi.Data;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly EduContext Context;

    protected RepositoryBase(EduContext context)
    {
        Context = context;
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        return await Context.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Context.Set<T>().ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await Context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        Context.Set<T>().Update(entity);
        await Context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync();
    }
}