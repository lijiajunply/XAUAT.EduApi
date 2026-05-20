using System.Text.Json;
using EduApi.Data.Models;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XAUAT.EduApi.Services;

public interface IPaymentService
{
    Task<string> Login(string cardNum, string language = "zh");
    Task<PaymentData> GetTurnoverAsync(string cardNum, string language = "zh");
}

public class PaymentService(
    ICacheService cacheService,
    IHttpClientFactory httpClientFactory,
    ILogger<PaymentService> logger,
    ITestAccountResolver? testAccountResolver = null,
    ITestDataProvider? testDataProvider = null)
    : IPaymentService
{
    public async Task<string> Login(string cardNum, string language = "zh")
    {
        if (testAccountResolver?.IsTestAccount(cardNum: cardNum) == true)
        {
            logger.LogInformation("测试账号命中支付登录测试数据，cardNum: {CardNum}", cardNum);
            return await testDataProvider!.GetPaymentTokenAsync();
        }

        try
        {
            return await cacheService.GetOrCreateAsync(
                CacheKeys.PaymentToken(cardNum),
                async () =>
                {
                    const string url = "https://ydfwpt.xauat.edu.cn/berserker-auth/oauth/token";

                    var payload = new
                    {
                        username = cardNum,
                        grant_type = "password",
                        scope = "all",
                        loginFrom = "h5",
                        logintype = "snoNew",
                        device_token = "h5",
                        synAccessSource = "h5"
                    };

                    var headers = new
                    {
                        ContentType = "application/x-www-form-urlencoded",
                        Authorization = "Basic bW9iaWxlX3NlcnZpY2VfcGxhdGZvcm06bW9iaWxlX3NlcnZpY2VfcGxhdGZvcm1fc2VjcmV0"
                    };

                    using var client = httpClientFactory.CreateClient("PaymentClient");
                    client.Timeout = HttpTimeouts.Slow;

                    var keyboardResponse =
                        await client.GetAsync(
                            "https://ydfwpt.xauat.edu.cn/berserker-secure/keyboard?type=Standard&order=0&synAccessSource=h5");
                    keyboardResponse.EnsureSuccessStatusCode();
                    var keyboardJson = await keyboardResponse.Content.ReadAsStringAsync();
                    var keyboardData = JObject.Parse(keyboardJson)["data"];
                    var num = keyboardData?["numberKeyboard"]?.ToObject<string>();
                    var password = $"{num?[2]}{num?[0]}{num?[2]}{num?[4]}{num?[1]}{num?[1]}$1${keyboardData?["uuid"]}";

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
                    return responseData["access_token"]!.ToObject<string>() ?? "";
                },
                TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            throw new PaymentServiceException($"登录失败: {ex.Message}", ex);
        }
    }

    public async Task<PaymentData> GetTurnoverAsync(string cardNum, string language = "zh")
    {
        if (testAccountResolver?.IsTestAccount(cardNum: cardNum) == true)
        {
            logger.LogInformation("测试账号命中支付流水测试数据，cardNum: {CardNum}", cardNum);
            var paymentsTask = testDataProvider!.GetPaymentTurnoverAsync();
            var balanceTask = testDataProvider.GetPaymentBalanceAsync();

            await Task.WhenAll(paymentsTask, balanceTask).ConfigureAwait(false);

            return new PaymentData
            {
                Records = await paymentsTask.ConfigureAwait(false),
                Total = await balanceTask.ConfigureAwait(false)
            };
        }

        try
        {
            var token = await Login(cardNum, language);

            // 并行获取支付列表和余额，解决 N+1 问题
            var paymentsTask = GetPayments(token, cardNum);
            var balanceTask = GetBalanceAsync(token, cardNum);

            await Task.WhenAll(paymentsTask, balanceTask).ConfigureAwait(false);

            return new PaymentData()
            {
                Records = await paymentsTask,
                Total = await balanceTask
            };
        }
        catch (Exception ex)
        {
            throw new PaymentServiceException($"获取消费记录失败: {ex.Message}", ex);
        }
    }

    private async Task<List<PaymentModel>> GetPayments(string token, string cardNum)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.PaymentList(cardNum),
            async () =>
            {
                const string url = "http://ydfwpt.xauat.edu.cn/berserker-search/search/personal/turnover";
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

                var uriBuilder = new UriBuilder(url);
                var query = string.Join("&", paramsDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                uriBuilder.Query = query;

                using var client = httpClientFactory.CreateClient("PaymentClient");
                client.Timeout = HttpTimeouts.EduSystem;
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
                    return records?.Select(PaymentModel.FromJson).ToList() ?? [];
                }

                logger.LogWarning("获取支付列表失败，状态码: {StatusCode}", response.StatusCode);
                return [];
            },
            TimeSpan.FromMinutes(20));
    }

    private async Task<double> GetBalanceAsync(string token, string cardNum)
    {
        return await cacheService.GetOrCreateAsync(
            CacheKeys.ElectronicAccount(cardNum),
            async () =>
            {
                const string url = "https://ydfwpt.xauat.edu.cn/berserker-app/ykt/tsm/queryCard?synAccessSource=h5";

                var headers = new Dictionary<string, string>
                {
                    { "synAccessSource", "h5" },
                    { "synjones-auth", token }
                };

                using var client = httpClientFactory.CreateClient("PaymentClient");
                client.Timeout = HttpTimeouts.EduSystem;
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode) return 0;
                var responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
                if (data == null || !data.TryGetValue("data", out var dataPart)) return 0;
                if (!dataPart.TryGetProperty("card", out var balanceElement) ||
                    !balanceElement[0].TryGetProperty("elec_accamt", out var element))
                {
                    return 0;
                }

                return element.GetDouble() / 100;
            },
            TimeSpan.FromMinutes(20),
            isUse: false);
    }
}
