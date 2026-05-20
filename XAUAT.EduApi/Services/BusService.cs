using EduApi.Data.Models;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Localization;

namespace XAUAT.EduApi.Services;

public interface IBusService
{
    Task<BusModel> GetBusFromOldDataAsync(string? time, string language, bool isShow = false);
    Task<BusModel> GetBusFromNewDataAsync(string? time, string loc, string language);
}

public class BusService(
    IHttpClientFactory httpClientFactory,
    ICacheService cacheService,
    IApiMessageLocalizer messageLocalizer) : IBusService
{
    public async Task<BusModel> GetBusFromOldDataAsync(string? time, string language, bool isShow = false)
    {
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        return await cacheService.GetOrCreateAsync(
            CacheKeys.Bus(time),
            async () =>
            {
                using var client = httpClientFactory.CreateClient("BusClient");
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
                    if (j["wayStation"] != null && !string.IsNullOrEmpty(j["wayStation"]!.ToString()))
                    {
                        continue;
                    }

                    busModel.Records.Add(new BusItem()
                    {
                        LineName = j["lineName"]!.ToString(),
                        Description = j["descr"]! + (isShow ? messageLocalizer.Get(language, ApiMessageKey.BusOldPlatformSuffix) : ""),
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

    public async Task<BusModel> GetBusFromNewDataAsync(string? time, string loc, string language)
    {
        time ??= DateTime.Today.ToString("yyyy-MM-dd");

        var busModel = await cacheService.GetOrCreateAsync(
            CacheKeys.BusNewData(time),
            async () =>
            {
                using var client = httpClientFactory.CreateClient("BusClient");
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
                    if (j["frcamp"] != null && !string.IsNullOrEmpty(j["frcamp"]!.ToString()))
                    {
                        continue;
                    }

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
            return await GetBusFromOldDataAsync(time, language, isShow: true);
        }

        return busModel;
    }
}
