using System.Globalization;
using System.Text.RegularExpressions;
using EduApi.Data;
using EduApi.Data.Models;
using HtmlAgilityPack;
using Polly;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Services;

public interface IExamService
{
    Task<ExamResponse> GetExamArrangementsAsync(
        string cookie,
        string? id,
        string language = "zh",
        IEnumerable<string>? requestStudentIds = null);

    Task<SemesterItem> GetThisSemester(string cookie, string language = "zh");
}

public class ExamService(
    IHttpClientFactory httpClientFactory,
    ILogger<ExamService> logger,
    IInfoService info,
    ICacheService cacheService,
    IExamRepository examRepository,
    ITestAccountResolver? testAccountResolver = null,
    ITestDataProvider? testDataProvider = null,
    IStudentRateLimitExecutor? rateLimitExecutor = null)
    : IExamService
{
    private const string BaseUrl = "https://swjw.xauat.edu.cn";

    private readonly IStudentRateLimitExecutor _rateLimitExecutor =
        rateLimitExecutor ?? NoOpStudentRateLimitExecutor.Instance;

    public async Task<ExamResponse> GetExamArrangementsAsync(
        string cookie,
        string? id,
        string language = "zh",
        IEnumerable<string>? requestStudentIds = null)
    {
        if (testAccountResolver?.IsTestAccount(cookie: cookie, studentId: id) == true)
        {
            logger.LogInformation("测试账号命中考试测试数据，studentId: {StudentId}", id);
            return await testDataProvider!.GetExamResponseAsync();
        }

        if (string.IsNullOrEmpty(id) || !id.Contains(','))
        {
            return await GetExamArrangementAsync(cookie, id, language, requestStudentIds);
        }

        var split = id.Split(',');

        // 使用 Task.WhenAll 并行获取所有学生的考试安排，解决 N+1 问题
        var tasks = split.Select(s => GetExamArrangementAsync(cookie, s, language, [s])).ToArray();
        var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 合并结果
        var examResponse = new ExamResponse();
        foreach (var result in allResults)
        {
            examResponse.Exams.AddRange(result.Exams);
        }

        return examResponse;
    }

    /// <summary>
    /// 获取本学期
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    public async Task<SemesterItem> GetThisSemester(string cookie, string language = "zh")
    {
        if (testAccountResolver?.IsTestAccount(cookie: cookie) == true)
        {
            logger.LogInformation("测试账号命中当前学期测试数据");
            return await testDataProvider!.GetCurrentSemesterAsync();
        }

        logger.LogInformation("开始抓取学期数据");

        return await cacheService.GetOrCreateAsync(
            CacheKeys.ThisSemester,
            async () =>
            {
                var retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .Or<TaskCanceledException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(6 * Math.Pow(2, retryAttempt - 1)));

                return await retryPolicy.ExecuteAsync(async () =>
                {
                    using var client = httpClientFactory.CreateClient();
                    client.SetRealisticHeaders();
                    client.Timeout = HttpTimeouts.EduSystem;
                    client.DefaultRequestHeaders.Add("Cookie", cookie);

                    var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/course-table");

                    html.ThrowIfAuthOrRateLimited();

                    return html.ParseNow(info);
                });
            },
            TimeSpan.FromHours(1));
    }

    /// <summary>
    /// 获取考试安排
    /// </summary>
    /// <param name="cookie"></param>
    /// <param name="id"></param>
    /// <param name="language"></param>
    /// <param name="requestStudentIds"></param>
    /// <returns></returns>
    private async Task<ExamResponse> GetExamArrangementAsync(
        string cookie,
        string? id = null,
        string language = "zh",
        IEnumerable<string>? requestStudentIds = null)
    {
        // 无id时不使用缓存
        if (string.IsNullOrEmpty(id))
        {
            return await FetchExamArrangementAsync(cookie, null, language, requestStudentIds);
        }

        return await cacheService.GetOrCreateAsync(
            CacheKeys.ExamArrangement(id),
            async () => await FetchExamArrangementAsync(cookie, id, language, requestStudentIds ?? [id]),
            TimeSpan.FromHours(1));
    }

    private async Task<ExamResponse> FetchExamArrangementAsync(
        string cookie,
        string? id,
        string language,
        IEnumerable<string>? requestStudentIds)
    {
        try
        {
            var url = $"{BaseUrl}/student/for-std/exam-arrange/";
            if (!string.IsNullOrEmpty(id))
            {
                url += $"info/{id}?";
            }

            var httpClient = httpClientFactory.CreateClient();
            httpClient.SetRealisticHeaders();
            httpClient.Timeout = HttpTimeouts.EduSystem;

            using var cts = new CancellationTokenSource(HttpTimeouts.EduSystem);

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(6 * Math.Pow(2, retryAttempt - 1)));

            return await _rateLimitExecutor.ExecuteAsync(
                requestStudentIds ?? [],
                () => retryPolicy.ExecuteAsync(async ct =>
                {
                    var requestPolicy = new HttpRequestMessage(HttpMethod.Get, url).WithCookie(cookie);
                    var response = await httpClient.SendAsync(requestPolicy, ct);
                    var content = await response.Content.ReadAsStringAsync(ct);

                    // 限流页识别必须放在执行器内部，这样第一次命中才能真正写入冷却状态
                    try
                    {
                        content.ThrowIfAuthOrRateLimited();
                    }
                    catch (UnAuthenticationError)
                    {
                        if (string.IsNullOrEmpty(id)) throw;
                        var data = await examRepository.GetByStudentIdAsync(id);

                        var examRecords = data as ExamRecord[] ?? data.ToArray();
                        if (examRecords.Length != 0)
                        {
                            return new ExamResponse()
                            {
                                Exams = examRecords.Select(x => new ExamInfo()
                                {
                                    Name = x.Name,
                                    Time = x.Time,
                                    Location = x.Location,
                                    Seat = x.Seat,
                                }).ToList()
                            };
                        }

                        throw;
                    }

                    var examData = ParseNow(content);
                    if (examData != null)
                    {
                        if (string.IsNullOrEmpty(id) || examData.Count <= 0)
                        {
                            return new ExamResponse
                            {
                                Exams = examData,
                            };
                        }

                        try
                        {
                            var records = examData.Select(i => ExamInfoToRecord(id, i)).ToList();
                            await examRepository.MergeStudentExamsAsync(id, records);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "合并考试数据到数据库时出错，studentId: {StudentId}", id);
                        }

                        return new ExamResponse
                        {
                            Exams = examData,
                        };
                    }

                    logger.LogWarning("Failed to deserialize exam data");
                    return new ExamResponse
                    {
                        Exams = [],
                        CanClick = false,
                        Error = "Failed to deserialize exam data"
                    };
                }, cts.Token));
        }
        catch (RateLimitException)
        {
            throw;
        }
        catch (UnAuthenticationError)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting exam arrangements");
            return new ExamResponse
            {
                Exams = [],
                CanClick = false,
                Error = ex.Message
            };
        }
    }

    private List<ExamInfo>? ParseNow(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var examList = new List<ExamInfo>();

        // 使用 XPath 定位到 id 为 exams 的表格下的 tbody 中的所有 tr 行
        var rows = doc.DocumentNode.SelectNodes("//table[@id='exams']/tbody/tr");

        if (rows != null!)
        {
            foreach (var cells in rows.Select(row => row.SelectNodes("td")))
            {
                // 确保单元格数量与表头一致（6列）
                if (cells is not { Count: >= 6 }) continue;
                var exam = new ExamInfo
                {
                    Name = cells[0].InnerText.Trim(),
                    Time = cells[1].InnerText.Trim(),
                    Location = cells[2].InnerText.Trim(),
                    Seat = cells[3].InnerText.Trim(),
                };
                examList.Add(exam);
            }
        }
        else
        {
            Console.WriteLine("⚠️ 提示：未在 <tbody> 中找到任何考试数据行（当前表格为空）。");
        }

        return examList;
    }

    private static ExamRecord ExamInfoToRecord(string studentId, ExamInfo info)
    {
        var timeRaw = info.Time;
        var examTime = ParseExamTime(timeRaw);
        var rawKey = $"{studentId}_{info.Name}_{timeRaw}";
        return new ExamRecord
        {
            Key = rawKey.ToHash(),
            StudentId = studentId,
            Name = info.Name,
            Time = timeRaw,
            ExamTime = examTime,
            Location = info.Location,
            Seat = info.Seat
        };
    }

    private static DateTime ParseExamTime(string timeRaw)
    {
        if (string.IsNullOrWhiteSpace(timeRaw))
            return DateTime.MinValue;

        var dashIndex = timeRaw.IndexOf('-');
        var startPart = dashIndex > 0 ? timeRaw[..dashIndex].Trim() : timeRaw.Trim();

        if (DateTime.TryParseExact(startPart, "yyyy-MM-dd HH:mm", null, DateTimeStyles.AssumeUniversal, out var result))
            return result;

        if (DateTime.TryParseExact(startPart, "yyyy/MM/dd HH:mm", null, DateTimeStyles.AssumeUniversal, out result))
            return result;

        if (DateTime.TryParse(startPart, null, DateTimeStyles.AssumeUniversal, out result))
            return result;

        return DateTime.MinValue;
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

        if (service.IsLessThanEnd())
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