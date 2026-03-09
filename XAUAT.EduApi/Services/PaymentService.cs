using System.Text.Json;
using EduApi.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XAUAT.EduApi.Services;

public interface IPaymentService
{
    Task<string> Login(string cardNum);
    Task<PaymentData> GetTurnoverAsync(string cardNum);
}

public class PaymentService : IPaymentService
{
    private readonly IDatabase? _redis;
    private readonly bool _redisAvailable;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IConnectionMultiplexer? muxer, IHttpClientFactory httpClientFactory, ILogger<PaymentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // 使用扩展方法初始化Redis连接
        _redis = muxer.SafeGetDatabase(logger, out _redisAvailable);
    }

    public async Task<string> Login(string cardNum)
    {
        try
        {
            // 使用统一的缓存键
            var cacheKey = CacheKeys.PaymentToken(cardNum);
            var cachedToken = await _redis.GetStringCacheAsync(_redisAvailable, cacheKey, _logger);
            if (!string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
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

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = HttpTimeouts.Slow; // 使用统一的慢速超时配置

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

            // 使用统一的缓存方法
            await _redis.SetStringCacheAsync(_redisAvailable, cacheKey, accessToken, TimeSpan.FromHours(1), _logger);
            return accessToken;
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentServiceException($"网络请求失败: 无法连接到支付服务，请稍后重试", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new PaymentServiceException($"请求超时: 支付服务响应超时，请稍后重试", ex);
        }
        catch (Exception ex)
        {
            throw new PaymentServiceException($"登录失败: {ex.Message}", ex);
        }
    }

    public async Task<PaymentData> GetTurnoverAsync(string cardNum)
    {
        try
        {
            var token = await Login(cardNum);

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
        try
        {
            // 使用统一的缓存键
            var cacheKey = CacheKeys.PaymentList(cardNum);
            var cachedPayments = await _redis.GetCacheAsync<List<PaymentModel>>(_redisAvailable, cacheKey, _logger);
            if (cachedPayments is { Count: > 0 })
            {
                return cachedPayments;
            }

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

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = HttpTimeouts.EduSystem; // 使用统一的超时配置
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

                // 使用统一的缓存方法
                await _redis.SetCacheAsync(_redisAvailable, cacheKey, a, TimeSpan.FromMinutes(20), _logger);
                return a;
            }

            _logger.LogWarning("获取支付列表失败，状态码: {StatusCode}", response.StatusCode);

            return [];
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentServiceException($"网络请求失败: 无法获取支付列表，请稍后重试", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new PaymentServiceException($"请求超时: 获取支付列表超时，请稍后重试", ex);
        }
        catch (Exception ex)
        {
            throw new PaymentServiceException($"获取支付列表失败: {ex.Message}", ex);
        }
    }

    private async Task<double> GetBalanceAsync(string token, string cardNum)
    {
        try
        {
            // 使用统一的缓存键
            var cacheKey = CacheKeys.ElectronicAccount(cardNum);
            var cachedBalance = await _redis.GetStringCacheAsync(_redisAvailable, cacheKey, _logger);
            if (!string.IsNullOrEmpty(cachedBalance) && double.TryParse(cachedBalance, out var balance))
            {
                return balance / 100;
            }

            const string url = "https://ydfwpt.xauat.edu.cn/berserker-app/ykt/tsm/queryCard?synAccessSource=h5";

            var headers = new Dictionary<string, string>
            {
                { "synAccessSource", "h5" },
                { "synjones-auth", token }
            };

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = HttpTimeouts.EduSystem; // 使用统一的超时配置
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
                !balanceElement[0].TryGetProperty("elec_accamt", out var element)) return 0;

            // 使用统一的缓存方法
            await _redis.SetStringCacheAsync(_redisAvailable, cacheKey, element.GetInt32().ToString(), TimeSpan.FromMinutes(20), _logger);
            return element.GetDouble() / 100;
        }
        catch (HttpRequestException ex)
        {
            throw new PaymentServiceException($"网络请求失败: 无法获取余额，请稍后重试", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new PaymentServiceException($"请求超时: 获取余额超时，请稍后重试", ex);
        }
        catch (Exception ex)
        {
            throw new PaymentServiceException($"获取余额失败: {ex.Message}", ex);
        }
    }
}
