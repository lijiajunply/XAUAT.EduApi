using CampusMapAPI.Models;

namespace CampusMapAPI.Services;

public interface IMapService
{
    Task<List<MapPoiModel>> GetAllPoisAsync();
    Task<List<MapPoiModel>> GetPoisByCategoryAsync(string category);
    Task<List<MapPoiModel>> GetPoisByCampusAsync(string campus);
    Task<MapPoiModel?> GetPoiByIdAsync(int id);
    Task<List<MapPoiModel>> SearchPoisAsync(string keyword);
    Task<List<string>> GetCategoriesAsync();
    Task<List<string>> GetCampusesAsync();
}
