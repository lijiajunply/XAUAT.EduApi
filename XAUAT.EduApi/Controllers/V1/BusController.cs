using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/bus")]
[Produces("application/json")]
public class BusController(
    IBusService busService,
    ILogger<BusController> logger,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer) : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet("{time?}")]
    [ProducesResponseType(typeof(ApiResponse<List<BusItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BusItem>>>> GetBus(string? time)
    {
        try
        {
            var result = await busService.GetBusFromOldDataAsync(time, Language);
            return Ok(SuccessListResponse(result.Records));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取校车时刻表失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.DataFetchFailed, Message(ApiMessageKey.BusFetchFailed)));
        }
    }

    [HttpGet("NewData/{time?}")]
    [ProducesResponseType(typeof(ApiResponse<List<BusItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BusItem>>>> GetBusFromNewData(string? time, string loc = "ALL")
    {
        try
        {
            var result = await busService.GetBusFromNewDataAsync(time, loc, Language);
            return Ok(SuccessListResponse(result.Records));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取新平台校车时刻表失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.DataFetchFailed, Message(ApiMessageKey.BusFetchFailed)));
        }
    }

    [HttpGet("OldData/{time?}")]
    [ProducesResponseType(typeof(ApiResponse<List<BusItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BusItem>>>> GetBusFromOldData(string? time, bool isShow = false)
    {
        try
        {
            var result = await busService.GetBusFromOldDataAsync(time, Language, isShow);
            return Ok(SuccessListResponse(result.Records));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取旧平台校车时刻表失败");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.DataFetchFailed, Message(ApiMessageKey.BusFetchFailed)));
        }
    }
}
