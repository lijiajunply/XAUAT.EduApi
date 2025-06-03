using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BusController(IHttpClientFactory httpClientFactory, IConnectionMultiplexer muxer)
    : ControllerBase
{
    private readonly IDatabase _redis = muxer.GetDatabase();

    [HttpGet("{time}")]
    public async Task<IActionResult> GetBus(string? time)
    {
        return Ok(await GetBusFromOldData(time));
    }

    /// <summary>
    /// 新数据平台
    /// </summary>
    /// <param name="loc">All,雁塔,草堂</param>
    /// <param name="time"></param>
    /// <returns></returns>
    [HttpGet("NewData/{time?}")]
    public async Task<IActionResult> GetBusFromNewData(string? time, string loc = "ALL")
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = new TimeSpan(0, 0, 0, 1, 0);
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var key = "bus_new_data:" + time;

        var bus = await _redis.StringGetAsync(key);

        if (bus.HasValue)
        {
            return Ok(JsonConvert.DeserializeObject<BusModel>(bus.ToString()));
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
            var time_tricks = tricks1970 + timestamp; //日志日期刻度

            var runTime = new DateTime(time_tricks);
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

        if (busModel.Total != 0)
            await _redis.StringSetAsync(key, JsonConvert.SerializeObject(busModel),
                expiry: new TimeSpan(0, 12, 0, 0));

        return Ok(busModel);
    }

    [HttpGet("OldData/{time?}")]
    public async Task<BusModel> GetBusFromOldData(string? time, bool isShow = false)
    {
        using var client = httpClientFactory.CreateClient();
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var bus = await _redis.StringGetAsync("bus:" + time);

        if (bus.HasValue)
        {
            return JsonConvert.DeserializeObject<BusModel>(bus.ToString()) ?? new BusModel();
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

        if (busModel.Total != 0)
            await _redis.StringSetAsync("bus:" + time, JsonConvert.SerializeObject(busModel),
                expiry: new TimeSpan(0, 12, 0, 0));

        return busModel;
    }
}