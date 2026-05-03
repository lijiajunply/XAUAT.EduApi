using System.Text.Json;
using EduApi.Data.Models;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;

namespace XAUAT.EduApi.Services;

public interface ISchoolNavService
{
    public Task<List<CategoryModel>> GetCategoriesAsync();
}

public class SchoolNavService(
    IHttpClientFactory httpClientFactory,
    ILogger<ExamService> logger,
    ICacheService cacheService) : ISchoolNavService
{
    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        return await cacheService.GetOrCreateAsync(CacheKeys.Categories(), async () =>
        {
            var list = new List<CategoryModel>();
            try
            {
                using var client = httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://link.xauat.site/Category");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<CategoryModel>>(json);
                if (result != null)
                {
                    list.AddRange(result);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting categories");
            }

            list.Sort((a, b) => a.Index.CompareTo(b.Index));
            return list;
        });
    }
}