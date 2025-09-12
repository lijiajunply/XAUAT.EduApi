using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Services;

public interface IExamService
{
    Task<ExamResponse> GetExamArrangementsAsync(string cookie,string? id);

    Task<SemesterItem> GetThisSemester(string cookie);
}

public class ExamService(
    IHttpClientFactory httpClientFactory,
    ILogger<ExamService> logger,
    IInfoService info,
    IConnectionMultiplexer muxer)
    : IExamService
{
    private const string _baseUrl = "https://swjw.xauat.edu.cn";
    private readonly IDatabase _redis = muxer.GetDatabase();

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
        logger.LogInformation("开始抓取学期数据");

        var thisSemester = await _redis.StringGetAsync("thisSemester");

        if (thisSemester is { HasValue: true, IsNullOrEmpty: false })
        {
            var item = JsonConvert.DeserializeObject<SemesterItem>(thisSemester.ToString()) ?? new SemesterItem();
            if (!string.IsNullOrEmpty(item.Value))
            {
                logger.LogInformation("已提取到缓存信息");
                return item;
            }
        }

        using var client = httpClientFactory.CreateClient();
        client.SetRealisticHeaders();
        client.Timeout = TimeSpan.FromSeconds(15); // 添加超时控制
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/course-table");
        var result = new SemesterResult();
        var data = result.ParseNow(html, info);

        await _redis.StringSetAsync("thisSemester", JsonConvert.SerializeObject(data),
            expiry: new TimeSpan(2, 0, 0, 0));

        return data;
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
            var cacheKey = $"exam_arrangement_{id}";
            if (!string.IsNullOrEmpty(id) && _redis.KeyExists(cacheKey))
            {
                var redisResult = _redis.StringGet(cacheKey);
                if (redisResult.HasValue)
                {
                    var a = JsonConvert.DeserializeObject<ExamResponse>(redisResult.ToString());
                    if (a is { CanClick: true }) return a;
                }
            }

            var url = $"{_baseUrl}/student/for-std/exam-arrange/";
            if (!string.IsNullOrEmpty(id))
            {
                url += $"info/{id}?";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookie);

            using var httpClient = httpClientFactory.CreateClient(); // 使用命名客户端
            httpClient.SetRealisticHeaders();

            // 设置 CancellationToken
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            // 添加重试逻辑（示例）
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
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = false
                };
            }

            // 解析数据
            var match = Regex.Match(content, @"var studentExamInfoVms = (.*?)\];", RegexOptions.Singleline);
            if (!match.Success)
            {
                logger.LogWarning("Failed to match exam data pattern");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = true
                };
            }

            var jsonData = match.Groups[1].Value + "]";
            // 替换单引号为双引号
            jsonData = jsonData.Replace("'", "\"");

            var examData = JsonConvert.DeserializeObject<List<ExamDataRaw>>(jsonData);
            if (examData == null)
            {
                logger.LogWarning("Failed to deserialize exam data");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = false
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
            jsonData = JsonConvert.SerializeObject(result);

            await _redis.StringSetAsync(cacheKey, jsonData,
                expiry: new TimeSpan(0, 1, 0, 0));

            return result;

            HttpRequestMessage CreateRequest() => new(HttpMethod.Get, url)
            {
                Headers = { { "Cookie", cookie } }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting exam arrangements");
            return new ExamResponse
            {
                Exams = [],
                CanClick = false
            };
        }
    }
}