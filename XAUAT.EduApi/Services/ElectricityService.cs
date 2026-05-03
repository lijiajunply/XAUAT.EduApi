using System.Globalization;
using EduApi.Data.Models;
using HtmlAgilityPack;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;

namespace XAUAT.EduApi.Services;

public interface IElectricityService
{
    public Task<double?> FetchCurrentBalanceAsync(string? url = null);
    public Task<List<ElectricData>> FetchWeeklyDataAsync(string? url = null);
    public Task<string?> GetRechargeUrlAsync(string? url = null);
}

public class ElectricityService(
    IHttpClientFactory httpClientFactory,
    ICacheService cacheService) : IElectricityService
{
    private static readonly TimeSpan BalanceCacheExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan WeeklyDataCacheExpiration = TimeSpan.FromMinutes(30);

    public async Task<double?> FetchCurrentBalanceAsync(string? url = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var balance = await cacheService.GetOrCreateAsync(
                CacheKeys.ElectricityBalance(url),
                async () => await FetchBalanceFromRemoteAsync(url),
                BalanceCacheExpiration);

            return balance;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ElectricData>> FetchWeeklyDataAsync(string? url = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return [];
        }

        var detailUrl = url.Replace("wxAccount", "wxElecDtl");
        return await cacheService.GetOrCreateAsync(
            CacheKeys.ElectricityWeeklyData(url),
            async () => await FetchWeeklyDataFromRemoteAsync(detailUrl),
            WeeklyDataCacheExpiration);
    }

    public Task<string?> GetRechargeUrlAsync(string? url = null)
    {
        try
        {
            return Task.FromResult(string.IsNullOrWhiteSpace(url) ? null : url.Replace("wxAccount", "wxCharge"));
        }
        catch (Exception exception)
        {
            return Task.FromException<string?>(exception);
        }
    }

    private async Task<double?> FetchBalanceFromRemoteAsync(string resolvedUrl)
    {
        using var httpClient = httpClientFactory.CreateClient();

        using var response = await httpClient.GetAsync(resolvedUrl);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var text = document.DocumentNode.InnerText;
        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        foreach (var line in lines)
        {
            const string keyword = "充值余额：¥";
            if (!line.Contains(keyword))
            {
                continue;
            }

            var balanceText = line.Split(keyword)[1].Trim();
            if (double.TryParse(balanceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var balance))
            {
                return balance;
            }
        }

        return null;
    }

    private async Task<List<ElectricData>> FetchWeeklyDataFromRemoteAsync(string detailUrl)
    {
        using var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(detailUrl);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var tables = document.DocumentNode.SelectNodes("//table");
        var data = new List<ElectricData>();

        if (tables == null!)
        {
            return data;
        }

        foreach (var cells in tables.Select(table => table.SelectNodes(".//tr"))
                     .Where(rows => rows != null!)
                     .SelectMany(rows => rows.Select(row => row.SelectNodes(".//td"))))
        {
            if (cells is not { Count: 3 })
            {
                continue;
            }

            var timestamp = ParseTimestamp(cells[1].InnerText.Trim());
            var valueText = cells[2].InnerText.Trim();

            if (timestamp == null ||
                !double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            if (data.Count == 0 || data[^1].Timestamp.Hour != timestamp.Value.Hour)
            {
                data.Add(new ElectricData
                {
                    Timestamp = timestamp.Value,
                    Value = value
                });
            }
            else
            {
                data[^1].Value += value;
            }
        }

        return data.OrderBy(x => x.Timestamp).ToList();
    }

    private static DateTime? ParseTimestamp(string rawValue)
    {
        var parts = rawValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        var datePart = parts[0];
        var timePart = parts[1];

        if (!DateTime.TryParseExact(
                $"{datePart} {timePart}",
                ["yyyy/M/d HH:mm", "yyyy/MM/dd HH:mm"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return null;
        }

        return new DateTime(result.Year, result.Month, result.Day, result.Hour, 0, 0);
    }
}