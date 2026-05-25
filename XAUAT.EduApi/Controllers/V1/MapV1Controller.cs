using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/map")]
[Produces("application/json")]
public class MapV1Controller(
    IMapService mapService,
    ILogger<MapV1Controller> logger,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer) : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MapPoiModel>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MapPoiModel>>>> GetAllPois()
    {
        try
        {
            var pois = await mapService.GetAllPoisAsync();
            return Ok(SuccessListResponse(pois));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取POI列表失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取POI数据失败"));
        }
    }

    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(ApiResponse<List<MapPoiModel>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MapPoiModel>>>> GetPoisByCategory(string category)
    {
        try
        {
            var pois = await mapService.GetPoisByCategoryAsync(category);
            if (pois.Count == 0)
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, $"未找到分类为 '{category}' 的POI"));
            }

            return Ok(SuccessListResponse(pois));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取分类POI失败: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, $"获取分类 '{category}' 的POI数据失败"));
        }
    }

    [HttpGet("campus/{campus}")]
    [ProducesResponseType(typeof(ApiResponse<List<MapPoiModel>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MapPoiModel>>>> GetPoisByCampus(string campus)
    {
        try
        {
            var pois = await mapService.GetPoisByCampusAsync(campus);
            if (pois.Count == 0)
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, $"未找到校区 '{campus}' 的POI"));
            }

            return Ok(SuccessListResponse(pois));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取校区POI失败: {Campus}", campus);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, $"获取校区 '{campus}' 的POI数据失败"));
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MapPoiModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MapPoiModel>>> GetPoiById(int id)
    {
        try
        {
            var poi = await mapService.GetPoiByIdAsync(id);
            if (poi == null)
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, $"未找到ID为 {id} 的POI"));
            }

            return Ok(SuccessResponse(poi));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取POI详情失败: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, $"获取ID为 {id} 的POI详情失败"));
        }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<MapPoiModel>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MapPoiModel>>>> SearchPois([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(ErrorResponse(ApiCodes.ParamError, "搜索关键词不能为空"));
        }

        try
        {
            var pois = await mapService.SearchPoisAsync(keyword);
            return Ok(SuccessListResponse(pois));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "搜索POI失败: {Keyword}", keyword);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "搜索POI数据失败"));
        }
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        try
        {
            var categories = await mapService.GetCategoriesAsync();
            return Ok(SuccessListResponse(categories));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取地图分类失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取分类数据失败"));
        }
    }

    [HttpGet("campuses")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCampuses()
    {
        try
        {
            var campuses = await mapService.GetCampusesAsync();
            return Ok(SuccessListResponse(campuses));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取校区列表失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取校区数据失败"));
        }
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ImportPoi([FromBody] MapPoiModel poi)
    {
        if (string.IsNullOrWhiteSpace(poi.Name) || string.IsNullOrWhiteSpace(poi.Category))
        {
            return BadRequest(ErrorResponse(ApiCodes.ParamError, "名称和分类不能为空"));
        }

        if (poi.Latitude == 0 || poi.Longitude == 0)
        {
            return BadRequest(ErrorResponse(ApiCodes.ParamError, "经纬度不能为0"));
        }

        try
        {
            await mapService.AddPoiAsync(poi);
            logger.LogInformation("成功导入POI: {Name} ({Latitude}, {Longitude})", poi.Name, poi.Latitude, poi.Longitude);
            return Ok(SuccessResponse<object>(new { success = true, message = $"成功导入: {poi.Name}", id = poi.Id }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "导入POI失败: {Name}", poi.Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, $"导入失败: {ex.Message}"));
        }
    }

    [HttpPost("import/batch")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ImportPoisBatch([FromBody] List<MapPoiModel> pois)
    {
        if (pois == null! || pois.Count == 0)
        {
            return BadRequest(ErrorResponse(ApiCodes.ParamError, "POI列表不能为空"));
        }

        var validPois = new List<MapPoiModel>();
        var errorCount = 0;
        var errors = new List<string>();

        foreach (var poi in pois)
        {
            if (string.IsNullOrWhiteSpace(poi.Name) || string.IsNullOrWhiteSpace(poi.Category))
            {
                errorCount++;
                errors.Add($"{poi.Name}: 名称或分类为空");
                continue;
            }

            if (poi.Latitude == 0 || poi.Longitude == 0)
            {
                errorCount++;
                errors.Add($"{poi.Name}: 经纬度无效");
                continue;
            }

            poi.IsActive = true;
            poi.CreatedAt = DateTime.UtcNow;
            poi.UpdatedAt = DateTime.UtcNow;
            validPois.Add(poi);
        }

        var successCount = validPois.Count;

        if (successCount > 0)
        {
            await mapService.AddPoisBatchAsync(validPois);
            logger.LogInformation("批量导入完成: 成功 {Success} 条, 失败 {Error} 条", successCount, errorCount);
        }

        return Ok(SuccessResponse<object>(new
        {
            imported_count = successCount,
            error_count = errorCount,
            errors = errors.Take(10),
            message = $"导入完成: 成功 {successCount} 条, 失败 {errorCount} 条"
        }));
    }

    [HttpDelete("clear")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ClearAllPois()
    {
        try
        {
            var count = await mapService.ClearAllPoisAsync();
            logger.LogWarning("已清空所有POI数据，共 {Count} 条", count);
            return Ok(SuccessResponse<object>(new { success = true, deleted_count = count, message = $"已清空 {count} 条POI数据" }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "清空POI数据失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, $"清空失败: {ex.Message}"));
        }
    }
}
