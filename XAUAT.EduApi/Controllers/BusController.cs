using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
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
    private readonly IDatabase? _redis;
    private readonly bool _redisAvailable;
    
    /// <summary>
    /// BusController构造函数
    /// </summary>
    /// <param name="httpClientFactory">HttpClient工厂，用于创建HTTP客户端</param>
    /// <param name="muxer">Redis连接多路复用器，用于缓存数据</param>
    /// <param name="logger">日志记录器，用于记录日志信息</param>
    public BusController(IHttpClientFactory httpClientFactory, IConnectionMultiplexer? muxer, ILogger<BusController>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _redisAvailable = muxer != null;
        if (_redisAvailable && muxer != null)
        {
            try
            {
                _redis = muxer.GetDatabase();
                logger?.LogInformation("Redis连接成功");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis连接失败");
                _redisAvailable = false;
            }
        }
        else
        {
            logger?.LogWarning("Redis未配置");
        }
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
    public async Task<IActionResult> GetBus(string? time)
    {
        return Ok(await GetBusFromOldData(time));
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
    public async Task<IActionResult> GetBusFromNewData(string? time, string loc = "ALL")
    {
        // 使用专门配置的HttpClient（跳过SSL验证）
        using var client = _httpClientFactory.CreateClient("BusClient");
        client.SetRealisticHeaders();
        client.Timeout = new TimeSpan(0, 0, 0, 1, 0);
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var key = "bus_new_data:" + time;

        // 尝试从Redis获取数据
        if (_redisAvailable && _redis != null)
        {
            var bus = await _redis.StringGetAsync(key);
            if (bus.HasValue)
            {
                return Ok(JsonConvert.DeserializeObject<BusModel>(bus.ToString()));
            }
        }

        var response =
            await client.PostAsJsonAsync(
                "https://bcdd.xauat.edu.cn/api/openapi/getDayBusPlans", new { type = loc, nowDay = time });
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var busModel = new BusModel();
        var json = JObject.Parse(content);

        var i = json["data"]!["dfBusPlans"] as JArray;

        if (i?.Count == 0)
        {
            return Ok(await GetBusFromOldData(time, isShow: true));
        }

        foreach (var j in json["data"]!["dfBusPlans"]!)
        {
            if (j["frcamp"] != null && !string.IsNullOrEmpty(j["frcamp"]!.ToString())) continue;
            var departure = j["fscamp"] + "校区";
            var arrival = j["fecamp"] + "校区";
            var timestamp = long.Parse(j["fstime"]!.ToString()) * 10000;

            var tricks1970 = new DateTime(1970, 1, 1, 8, 0, 0).Ticks; //1970年1月1日刻度
            var timeTricks = tricks1970 + timestamp; //日志日期刻度

            var runTime = new DateTime(timeTricks);
            busModel.Records.Add(new BusItem()
            {
                LineName = $"{departure}→{arrival}",
                Description = j["fbusNo"]!.ToString(),
                DepartureStation = departure,
                ArrivalStation = arrival,
                RunTime = runTime.ToString("T"),
                ArrivalStationTime = "01:30",
            });
        }

        busModel.Total = busModel.Records.Count;

        if (busModel.Total != 0 && _redisAvailable && _redis != null)
            await _redis.StringSetAsync(key, JsonConvert.SerializeObject(busModel),
                expiry: new TimeSpan(0, 12, 0, 0));

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
    public async Task<BusModel> GetBusFromOldData(string? time, bool isShow = false)
    {
        // 使用专门配置的HttpClient（跳过SSL验证）
        using var client = _httpClientFactory.CreateClient("BusClient");
        client.SetRealisticHeaders();
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        // 尝试从Redis获取数据
        if (_redisAvailable && _redis != null)
        {
            var bus = await _redis.StringGetAsync("bus:" + time);
            if (bus.HasValue)
            {
                return JsonConvert.DeserializeObject<BusModel>(bus.ToString()) ?? new BusModel();
            }
        }

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

        if (busModel.Total != 0 && _redisAvailable && _redis != null)
            await _redis.StringSetAsync("bus:" + time, JsonConvert.SerializeObject(busModel),
                expiry: new TimeSpan(0, 12, 0, 0));

        return busModel;
    }
}