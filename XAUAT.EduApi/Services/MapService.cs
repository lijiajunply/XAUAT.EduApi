using System.Linq.Expressions;
using CampusMapAPI.Models;
using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;

namespace CampusMapAPI.Services;

public class MapService(EduContext dbContext, ICacheService cacheService) : IMapService
{
    public async Task<List<MapPoiModel>> GetAllPoisAsync()
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapPois(),
            async () =>
            {
                return await dbContext.MapPois
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            },
            TimeSpan.FromHours(24));
    }

    public async Task<List<MapPoiModel>> GetPoisByCategoryAsync(string category)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapPoiCategory(category),
            async () =>
            {
                return await dbContext.MapPois
                    .Where(p => p.IsActive && p.Category == category)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            },
            TimeSpan.FromHours(24));
    }

    public async Task<List<MapPoiModel>> GetPoisByCampusAsync(string campus)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapPoiCampus(campus),
            async () =>
            {
                return await dbContext.MapPois
                    .Where(p => p.IsActive && p.Campus == campus)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            },
            TimeSpan.FromHours(24));
    }

    public async Task<MapPoiModel?> GetPoiByIdAsync(int id)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapPoiDetail(id),
            async () =>
            {
                return await dbContext.MapPois.FindAsync(id);
            },
            TimeSpan.FromHours(24));
    }

    public async Task<List<MapPoiModel>> SearchPoisAsync(string keyword)
    {
        keyword = keyword.ToLower().Trim();

        return await dbContext.MapPois
            .Where(p => p.IsActive &&
                (p.Name.ToLower().Contains(keyword) ||
                 (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                 (p.Address != null && p.Address.ToLower().Contains(keyword)) ||
                 p.Category.ToLower().Contains(keyword)))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapCategories(),
            async () =>
            {
                return await dbContext.MapPois
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            },
            TimeSpan.FromHours(24));
    }

    public async Task<List<string>> GetCampusesAsync()
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.MapCampuses(),
            async () =>
            {
                return await dbContext.MapPois
                    .Where(p => p.IsActive && p.Campus != null)
                    .Select(p => p.Campus!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            },
            TimeSpan.FromHours(24));
    }
}
