using System.Text.RegularExpressions;
using EduApi.Data.Models;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;

namespace XAUAT.EduApi.Services;

public interface IExamService
{
    Task<ExamResponse> GetExamArrangementsAsync(string cookie, string? id);

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

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var client = httpClientFactory.CreateClient();
            client.SetRealisticHeaders();
            client.Timeout = TimeSpan.FromSeconds(5); // 修改为5秒超时
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/course-table");

            var data = html.ParseNow(info);

            await _redis.StringSetAsync("thisSemester", JsonConvert.SerializeObject(data),
                expiry: new TimeSpan(2, 0, 0, 0));

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
            httpClient.Timeout = TimeSpan.FromSeconds(5); // 修改为5秒超时

            // 设置 CancellationToken
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
            jsonData = Regex.Replace(jsonData, @"\\x[0-9A-Fa-f]{2}", "");

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