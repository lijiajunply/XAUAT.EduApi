using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 校车时刻表控制器
/// 提供校车运行时刻表查询功能，支持从新旧两个数据平台获取数据
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BusController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// BusController构造函数
    /// </summary>
    /// <param name="httpClientFactory">HttpClient工厂，用于创建HTTP客户端</param>
    /// <param name="cacheService">缓存服务</param>
    public BusController(IHttpClientFactory httpClientFactory, ICacheService cacheService)
    {
        _httpClientFactory = httpClientFactory;
        _cacheService = cacheService;
    }

    /// <summary>
    /// 获取校车时刻表
    /// 从旧数据平台获取指定日期的校车运行时刻表
    /// </summary>
    /// <param name="time">可选，查询日期，格式为YYYY-MM-DD，默认为当天</param>
    /// <returns>校车时刻表数据，包含所有线路的发车时间、站点等信息</returns>
    /// <response code="200">成功获取校车时刻表</response>
    /// <response code="500">服务器内部错误，无法获取校车数据</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Bus
    ///
    /// 获取指定日期的校车时刻表：
    /// GET /Bus/2024-12-01
    /// </remarks>
    [HttpGet("{time?}")]
    [ProducesResponseType(typeof(BusModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BusModel>> GetBus(string? time)
    {
        return await GetBusFromOldData(time);
    }

    /// <summary>
    /// 从新数据平台获取校车时刻表
    /// 从新的校车数据平台获取指定日期和校区的校车运行时刻表
    /// </summary>
    /// <param name="time">可选，查询日期，格式为YYYY-MM-DD，默认为当天</param>
    /// <param name="loc">校区过滤条件，可选值：ALL（所有校区）、雁塔、草堂，默认为ALL</param>
    /// <returns>校车时刻表数据，包含所有线路的发车时间、站点等信息</returns>
    /// <response code="200">成功获取校车时刻表</response>
    /// <response code="500">服务器内部错误，无法获取校车数据</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Bus/NewData
    ///
    /// 获取指定日期和校区的校车时刻表：
    /// GET /Bus/NewData/2024-12-01?loc=雁塔
    /// </remarks>
    [HttpGet("NewData/{time?}")]
    [ProducesResponseType(typeof(BusModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BusModel>> GetBusFromNewData(string? time, string loc = "ALL")
    {
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var busModel = await _cacheService.GetOrCreateAsync(
            CacheKeys.BusNewData(time),
            async () =>
            {
                using var client = _httpClientFactory.CreateClient("BusClient");
                client.SetRealisticHeaders();

                var response =
                    await client.PostAsJsonAsync(
                        "https://bcdd.xauat.edu.cn/api/openapi/getDayBusPlans", new { type = loc, nowDay = time });
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = new BusModel();
                var json = JObject.Parse(content);

                var i = json["data"]!["dfBusPlans"] as JArray;

                if (i == null || i.Count == 0)
                {
                    return result;
                }

                foreach (var j in json["data"]!["dfBusPlans"]!)
                {
                    if (j["frcamp"] != null && !string.IsNullOrEmpty(j["frcamp"]!.ToString())) continue;
                    var departure = j["fscamp"] + "校区";
                    var arrival = j["fecamp"] + "校区";
                    var timestamp = long.Parse(j["fstime"]!.ToString()) * 10000;

                    var tricks1970 = new DateTime(1970, 1, 1, 8, 0, 0).Ticks;
                    var timeTricks = tricks1970 + timestamp;

                    var runTime = new DateTime(timeTricks);
                    result.Records.Add(new BusItem()
                    {
                        LineName = $"{departure}→{arrival}",
                        Description = j["fbusNo"]!.ToString(),
                        DepartureStation = departure,
                        ArrivalStation = arrival,
                        RunTime = runTime.ToString("T"),
                        ArrivalStationTime = "01:30",
                    });
                }

                result.Total = result.Records.Count;
                return result;
            },
            TimeSpan.FromHours(12));

        if (busModel.Total == 0)
        {
            return await GetBusFromOldData(time, isShow: true);
        }

        return Ok(busModel);
    }

    /// <summary>
    /// 从旧数据平台获取校车时刻表
    /// 从旧的校车数据平台获取指定日期的校车运行时刻表
    /// </summary>
    /// <param name="time">可选，查询日期，格式为YYYY-MM-DD，默认为当天</param>
    /// <param name="isShow">可选，是否显示数据来源标识，默认为false</param>
    /// <returns>校车时刻表数据，包含所有线路的发车时间、站点等信息</returns>
    [HttpGet("OldData/{time?}")]
    [ProducesResponseType(typeof(BusModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<BusModel>> GetBusFromOldData(string? time, bool isShow = false)
    {
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        return await _cacheService.GetOrCreateAsync(
            CacheKeys.Bus(time),
            async () =>
            {
                using var client = _httpClientFactory.CreateClient("BusClient");
                client.SetRealisticHeaders();

                var response =
                    await client.GetAsync(
                        $"https://school-bus.xauat.edu.cn/api/school/bus/user/runPlanPage?current=1&size=30&keyWord=&lineId=&date={time}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var busModel = new BusModel();
                var json = JObject.Parse(content);
                busModel.Total = json["data"]!["total"]!.ToObject<int>();
                foreach (var j in json["data"]!["records"]!)
                {
                    if (j["wayStation"] != null && !string.IsNullOrEmpty(j["wayStation"]!.ToString())) continue;
                    busModel.Records.Add(new BusItem()
                    {
                        LineName = j["lineName"]!.ToString(),
                        Description = j["descr"]! + (isShow ? " (调用的为旧平台数据)" : ""),
                        DepartureStation = j["departureStation"]!.ToString(),
                        ArrivalStation = j["arrivalStation"]!.ToString(),
                        RunTime = j["runTime"]!.ToString(),
                        ArrivalStationTime = j["arrivalStationTime"]!.ToString()
                    });
                }

                return busModel;
            },
            TimeSpan.FromHours(12));
    }
}
