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

    [HttpGet("{time?}")]
    public async Task<IActionResult> GetBus(string? time)
    {
        var client = httpClientFactory.CreateClient();
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var bus = await _redis.StringGetAsync("bus:" + time);

        if (bus.HasValue)
        {
            return Ok(JsonConvert.DeserializeObject<BusModel>(bus.ToString()));
        }

        var response =
            await client.GetAsync(
                $"https://school-bus.xauat.edu.cn/api/school/bus/user/runPlanPage?current=1&size=20&keyWord=&lineId=&date={time}");
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
                Description = j["descr"]!.ToString(),
                DepartureStation = j["departureStation"]!.ToString(),
                ArrivalStation = j["arrivalStation"]!.ToString(),
                RunTime = j["runTime"]!.ToString(),
                ArrivalStationTime = j["arrivalStationTime"]!.ToString()
            });
        }

        if (busModel.Total != 0)
            await _redis.StringSetAsync("bus:" + time, JsonConvert.SerializeObject(busModel),
                expiry: new TimeSpan(0, 6, 0, 0));

        return Ok(busModel);
    }
}