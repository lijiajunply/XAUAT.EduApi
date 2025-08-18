using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XAUAT.EduApi.Services;

public interface IPaymentService
{
    Task<string> Login(string cardNum);
    Task<PaymentData> GetTurnoverAsync(string cardNum);
}

public class PaymentService(IConnectionMultiplexer muxer, IHttpClientFactory httpClientFactory) : IPaymentService
{
    private readonly IDatabase _redis = muxer.GetDatabase();

    public async Task<string> Login(string cardNum)
    {
        var key = $"payment-{cardNum}";
        var paymentValue = await _redis.StringGetAsync(key);

        if (paymentValue is { HasValue: true, IsNullOrEmpty: false })
        {
            var item = paymentValue.ToString();
            if (!string.IsNullOrEmpty(item))
            {
                return item;
            }
        }

        const string url = "https://ydfwpt.xauat.edu.cn/berserker-auth/oauth/token";

        var payload = new
        {
            username = cardNum,
            grant_type = "password",
            scope = "all",
            loginFrom = "h5",
            logintype = "card",
            device_token = "h5",
            synAccessSource = "h5"
        };

        var headers = new
        {
            ContentType = "application/x-www-form-urlencoded",
            Authorization = "Basic bW9iaWxlX3NlcnZpY2VfcGxhdGZvcm06bW9iaWxlX3NlcnZpY2VfcGxhdGZvcm1fc2VjcmV0"
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15); // 添加超时控制
        // Step 1: Get the keyboard data
        var keyboardResponse =
            await client.GetAsync(
                "https://ydfwpt.xauat.edu.cn/berserker-secure/keyboard?type=Standard&order=0&synAccessSource=h5");
        keyboardResponse.EnsureSuccessStatusCode();
        var keyboardJson = await keyboardResponse.Content.ReadAsStringAsync();
        var keyboardData = JObject.Parse(keyboardJson)["data"];
        var num = keyboardData?["numberKeyboard"]?.ToObject<string>();
        var password = $"{num?[2]}{num?[0]}{num?[2]}{num?[4]}{num?[1]}{num?[1]}$1${keyboardData?["uuid"]}";

        // Step 2: Prepare the payload with the generated password
        var finalPayload = new
        {
            payload.username,
            payload.grant_type,
            payload.scope,
            payload.loginFrom,
            payload.logintype,
            payload.device_token,
            payload.synAccessSource,
            password
        };

        // Step 3: Send the POST request
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("username", finalPayload.username),
            new KeyValuePair<string, string>("grant_type", finalPayload.grant_type),
            new KeyValuePair<string, string>("scope", finalPayload.scope),
            new KeyValuePair<string, string>("loginFrom", finalPayload.loginFrom),
            new KeyValuePair<string, string>("logintype", finalPayload.logintype),
            new KeyValuePair<string, string>("device_token", finalPayload.device_token),
            new KeyValuePair<string, string>("synAccessSource", finalPayload.synAccessSource),
            new KeyValuePair<string, string>("password", finalPayload.password)
        ]);

        client.DefaultRequestHeaders.Add("Authorization", headers.Authorization);
        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JObject.Parse(responseJson);
        var accessToken = responseData["access_token"]!.ToObject<string>() ?? "";

        await _redis.StringSetAsync(key, accessToken, TimeSpan.FromDays(1));
        return accessToken;
    }

    public async Task<PaymentData> GetTurnoverAsync(string cardNum)
    {
        var token = await Login(cardNum);
        var payments = await GetPayments(token, cardNum);
        var t = await GetBalanceAsync(token, cardNum);
        return new PaymentData()
        {
            Records = payments,
            Total = t
        };
    }

    private async Task<List<PaymentModel>> GetPayments(string token, string cardNum)
    {
        var key = $"paymentList-{cardNum}";
        var paymentValue = await _redis.StringGetAsync(key);

        if (paymentValue is { HasValue: true, IsNullOrEmpty: false })
        {
            var item = paymentValue.ToString();
            if (!string.IsNullOrEmpty(item))
            {
                return JsonConvert.DeserializeObject<List<PaymentModel>>(item) ?? [];
            }
        }

        const string _url = "http://ydfwpt.xauat.edu.cn/berserker-search/search/personal/turnover";
        var paramsDict = new Dictionary<string, string>
        {
            { "size", "8" },
            { "current", "1" },
            { "synAccessSource", "h5" }
        };

        var headers = new Dictionary<string, string>
        {
            { "synAccessSource", "h5" },
            { "synjones-auth", token }
        };

        var uriBuilder = new UriBuilder(_url);
        var query = string.Join("&", paramsDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        uriBuilder.Query = query;

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(6); // 添加超时控制
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var response = await client.GetAsync(uriBuilder.Uri);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

            if (data == null || !data.TryGetValue("data", out var dataPart) ||
                !dataPart.TryGetProperty("records", out var recordsElement)) return [];
            var records = recordsElement.Deserialize<List<Dictionary<string, JsonElement>>>();
            var a = records?.Select(PaymentModel.FromJson).ToList() ?? [];

            await _redis.StringSetAsync(key, JsonConvert.SerializeObject(a), TimeSpan.FromMinutes(20));
            return a;
        }

        Console.WriteLine($"Error: {response.StatusCode}");

        return [];
    }

    private async Task<double> GetBalanceAsync(string token, string cardNum)
    {
        var key = $"ele-acc-{cardNum}";
        var paymentValue = await _redis.StringGetAsync(key);

        if (paymentValue is { HasValue: true, IsNullOrEmpty: false })
        {
            var item = paymentValue.ToString();
            if (!string.IsNullOrEmpty(item))
            {
                return double.Parse(item) / 100;
            }
        }

        const string _url = "https://ydfwpt.xauat.edu.cn/berserker-app/ykt/tsm/queryCard?synAccessSource=h5";

        var headers = new Dictionary<string, string>
        {
            { "synAccessSource", "h5" },
            { "synjones-auth", token }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(6); // 添加超时控制
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var response = await client.GetAsync(_url);

        if (!response.IsSuccessStatusCode) return 0;
        var responseBody = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
        if (data == null || !data.TryGetValue("data", out var dataPart)) return 0;
        if (!dataPart.TryGetProperty("card", out var balanceElement) ||
            !balanceElement[0].TryGetProperty("elec_accamt", out var element)) return 0;
        await _redis.StringSetAsync(key, element.GetString() ?? "0", TimeSpan.FromMinutes(20));
        return element.GetDouble() / 100;
    }
}