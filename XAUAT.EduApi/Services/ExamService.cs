using System.Text.RegularExpressions;
using EduApi.Data.Models;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;
using XAUAT.EduApi.Extensions;

namespace XAUAT.EduApi.Services;

public interface IExamService
{
    Task<ExamResponse> GetExamArrangementsAsync(string cookie, string? id);

    Task<SemesterItem> GetThisSemester(string cookie);
}

public class ExamService : IExamService
{
    private const string BaseUrl = "https://swjw.xauat.edu.cn";
    private readonly IDatabase? _redis;
    private readonly bool _redisAvailable;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExamService> _logger;
    private readonly IInfoService _info;

    public ExamService(IHttpClientFactory httpClientFactory, ILogger<ExamService> logger, IInfoService info,
        IConnectionMultiplexer? muxer)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _info = info;

        // 使用扩展方法初始化Redis连接
        _redis = muxer.SafeGetDatabase(_logger, out _redisAvailable);
    }

    public async Task<ExamResponse> GetExamArrangementsAsync(string cookie, string? id)
    {
        if (string.IsNullOrEmpty(id) || !id.Contains(','))
        {
            return await GetExamArrangementAsync(cookie);
        }

        var split = id.Split(',');
        var examResponse = new ExamResponse();
        foreach (var s in split)
        {
            var e1 = await GetExamArrangementAsync(cookie, s);
            examResponse.Exams.AddRange(e1.Exams);
        }

        return examResponse;
    }

    /// <summary>
    /// 获取本学期
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    public async Task<SemesterItem> GetThisSemester(string cookie)
    {
        _logger.LogInformation("开始抓取学期数据");

        // 尝试从Redis获取缓存
        var cachedSemester = await _redis.GetCacheAsync<SemesterItem>(_redisAvailable, CacheKeys.ThisSemester, _logger);
        if (cachedSemester != null && !string.IsNullOrEmpty(cachedSemester.Value))
        {
            return cachedSemester;
        }

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var client = _httpClientFactory.CreateClient();
            client.SetRealisticHeaders();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/course-table");

            // 检查是否重定向到登录页面
            if (html.Contains("登入页面"))
            {
                throw new Exceptions.UnAuthenticationError();
            }

            var data = html.ParseNow(_info);

            // 缓存到Redis
            await _redis.SetCacheAsync(_redisAvailable, CacheKeys.ThisSemester, data, TimeSpan.FromHours(1), _logger);

            return data;
        });
    }

    /// <summary>
    /// 获取考试安排
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<ExamResponse> GetExamArrangementAsync(string cookie, string? id = null)
    {
        try
        {
            var cacheKey = CacheKeys.ExamArrangement(id);

            // 尝试从Redis获取缓存
            if (!string.IsNullOrEmpty(id))
            {
                var cachedExam = await _redis.GetCacheAsync<ExamResponse>(_redisAvailable, cacheKey, _logger);
                if (cachedExam is { CanClick: true })
                {
                    return cachedExam;
                }
            }

            var url = $"{BaseUrl}/student/for-std/exam-arrange/";
            if (!string.IsNullOrEmpty(id))
            {
                url += $"info/{id}?";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url).WithCookie(cookie);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.SetRealisticHeaders();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            // 设置 CancellationToken
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // 添加重试逻辑
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var response = await retryPolicy.ExecuteAsync(async ct =>
            {
                var requestPolicy = CreateRequest(); // 每次重试都新建请求
                return await httpClient.SendAsync(requestPolicy, ct);
            }, cts.Token);

            var content = await response.Content.ReadAsStringAsync(cts.Token);

            // 检查是否重定向到登录页面
            if (content.Contains("登入页面"))
            {
                throw new Exceptions.UnAuthenticationError();
            }

            // 解析数据
            var match = Regex.Match(content, @"var studentExamInfoVms = (.*?)\];", RegexOptions.Singleline);
            if (!match.Success)
            {
                _logger.LogWarning("Failed to match exam data pattern");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = false,
                    Error = "Failed to match exam data pattern"
                };
            }

            var jsonData = match.Groups[1].Value + "]";
            // 替换单引号为双引号
            jsonData = jsonData.Replace("'", "\"");
            jsonData = Regex.Replace(jsonData, @"\\x[0-9A-Fa-f]{2}", "");

            var examData = JsonConvert.DeserializeObject<List<ExamDataRaw>>(jsonData);
            if (examData == null)
            {
                _logger.LogWarning("Failed to deserialize exam data");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = false,
                    Error = "Failed to deserialize exam data"
                };
            }

            var result = new ExamResponse
            {
                Exams = examData.Select(d => new ExamInfo
                {
                    Name = d.Course.NameZh,
                    Time = d.ExamTime,
                    Location = d.Room,
                    Seat = d.SeatNo
                }).ToList(),
                CanClick = examData.Count != 0
            };

            if (string.IsNullOrEmpty(id)) return result;

            // 缓存到Redis
            await _redis.SetCacheAsync(_redisAvailable, cacheKey, result, TimeSpan.FromHours(1), _logger);

            return result;

            HttpRequestMessage CreateRequest() => new HttpRequestMessage(HttpMethod.Get, url).WithCookie(cookie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam arrangements");
            return new ExamResponse
            {
                Exams = [],
                CanClick = false,
                Error = ex.Message
            };
        }
    }
}

public static class SemesterModelStatic
{
    public static SemesterItem ParseNow(this string html, IInfoService service)
    {
        // 检查是否登录
        if (!Regex.IsMatch(html, "课表"))
        {
            return new SemesterItem();
        }

        // 使用正则表达式匹配所有semester选项
        var regexString = "<option value=\"(.*)\">(.*)</option>";

        if (service.IsInSchool())
        {
            regexString = "<option selected=\"selected\" value=\"(.*)\">(.*)</option>";
        }

        var regex = new Regex(regexString);
        var matches = regex.Matches(html);

        if (matches.Count == 0) return new SemesterItem();

        var text = matches.First().Groups[2].Value;

        if (text == "" || text[^1] == '3')
        {
            return new SemesterItem()
            {
                Value = "301",
                Text = "2025-2026-1"
            };
        }

        return new SemesterItem()
        {
            Value = matches.First().Groups[1].Value,
            Text = text
        };
    }
}
