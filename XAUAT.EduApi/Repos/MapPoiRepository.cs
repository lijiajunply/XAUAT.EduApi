using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class MapPoiRepository(IDbContextFactory<EduContext> contextFactory) : IMapPoiRepository
{
    public async Task<List<MapPoiModel>> GetAllActiveAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<MapPoiModel>> GetByCategoryAsync(string category)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive && p.Category == category)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<MapPoiModel>> GetByCampusAsync(string campus)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive && p.Campus == campus)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<MapPoiModel?> GetByIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<List<MapPoiModel>> SearchAsync(string keyword)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive &&
                        (p.Name.ToLower().Contains(keyword) ||
                         (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                         (p.Address != null && p.Address.ToLower().Contains(keyword)) ||
                         p.Category.ToLower().Contains(keyword)))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Take(50)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<List<string>> GetCampusesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MapPois
            .AsNoTracking()
            .Where(p => p.IsActive && p.Campus != null)
            .Select(p => p.Campus!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(MapPoiModel poi)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.MapPois.AddAsync(poi).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<MapPoiModel> pois)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.MapPois.AddRangeAsync(pois).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(MapPoiModel poi)
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.MapPois.Update(poi);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<int> RemoveAllAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var all = await context.MapPois.ToListAsync().ConfigureAwait(false);
        context.MapPois.RemoveRange(all);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return all.Count;
    }
}
