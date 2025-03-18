using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BusController(IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    [HttpGet("{time?}")]
    public async Task<IActionResult> GetBus(string? time)
    {
        var client = httpClientFactory.CreateClient();
        time ??= DateTime.Today.ToString("yyyy-MM-dd");
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
        
        return Ok(busModel);
    }
}