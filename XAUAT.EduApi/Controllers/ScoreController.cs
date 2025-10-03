using System.Text.RegularExpressions;
using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ScoreController(
    IHttpClientFactory httpClientFactory,
    ILogger<ScoreController> logger,
    IExamService exam,
    IConnectionMultiplexer muxer,
    IDbContextFactory<EduContext> factory)
    : ControllerBase
{
    private readonly IDatabase _redis = muxer.GetDatabase();

    [HttpGet("Semester")]
    public async Task<ActionResult<SemesterResult>> ParseSemester(string? studentId)
    {
        try
        {
            logger.LogInformation("开始抓取学期数据");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            using var client = httpClientFactory.CreateClient();
            client.SetRealisticHeaders();
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            client.Timeout = TimeSpan.FromSeconds(3);
            var url = "https://swjw.xauat.edu.cn/student/for-std/grade/sheet";
            if (!string.IsNullOrEmpty(studentId))
            {
                var split = studentId.Split(',');
                studentId = split.FirstOrDefault();
                url += $"/semester-index/{studentId}";
            }

            var html = await client.GetStringAsync(url);
            var result = new SemesterResult();
            result.Parse(html);
            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("ThisSemester")]
    public async Task<ActionResult<SemesterItem>> GetThisSemester()
    {
        try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            return await exam.GetThisSemester(cookie);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ScoreResponse>>> GetScore(string studentId, string semester)
    {
        try
        {
            logger.LogInformation("开始抓取考试分数");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(cookie))
            {
                return BadRequest("学号或Cookie不能为空");
            }

            // 确保用户存在于数据库中
            await EnsureUserExists(studentId, cookie);

            var split = studentId.Split(',');
            var result = new List<ScoreResponse>();

            for (var i = 0; i < split.Length; i++)
            {
                var s = split[i];
                var scoreResponse = await GetScoreResponse(s, semester, cookie);
                var scoreResponses = scoreResponse as ScoreResponse[] ?? scoreResponse.ToArray();
                if (i != 0)
                {
                    foreach (var t in scoreResponses)
                    {
                        t.IsMinor = true;
                    }
                }

                result.AddRange(scoreResponses);
            }

            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 确保用户存在于数据库中
    /// </summary>
    /// <param name="studentId"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private async Task EnsureUserExists(string studentId, string cookie)
    {
        await using var context = await factory.CreateDbContextAsync();
        var userExists = await context.Users.AnyAsync(u => u.Id == studentId);

        if (!userExists)
        {
            // 如果用户不存在，创建一个新用户
            var newUser = new UserModel
            {
                Id = studentId,
                Username = studentId, // 使用学号作为默认用户名
                Password = "", // 密码留空，因为我们通过cookie验证
                SemesterUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ScoreResponsesUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();
            logger.LogInformation("为学号 {StudentId} 创建了新用户记录", studentId);
        }
    }

    /// <summary>
    /// 获取单学期成绩
    /// </summary>
    /// <param name="studentId"></param>
    /// <param name="semester"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private async Task<IEnumerable<ScoreResponse>> GetScoreResponse(string studentId, string semester, string cookie)
    {
        // 先判断是否为当前学期
        var thisSemester = await exam.GetThisSemester(cookie);
        var isCurrentSemester = thisSemester.Value == semester;

        // 如果是当前学期，直接爬虫
        if (isCurrentSemester)
        {
            return await CrawlScores(studentId, semester, cookie);
        }

        // 如果不是当前学期，先从数据库查询
        await using var context = await factory.CreateDbContextAsync();
        var dbScores = await context.Scores
            .Where(s => s.Key.StartsWith($"{studentId}_{semester}_"))
            .ToListAsync();

        // 如果数据库中有数据，直接返回
        if (dbScores.Count != 0)
        {
            return dbScores;
        }

        // 如果数据库中没有数据，则爬虫获取并保存到数据库
        var crawledScores = await CrawlScores(studentId, semester, cookie);
        foreach (var score in crawledScores)
        {
            // 为每个成绩项生成唯一键值
            score.Key = $"{studentId}_{semester}_{score.LessonCode}_{score.Name}".ToHash();
            context.Set<ScoreResponse>().Add(score);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "保存成绩数据到数据库时出错");
            // 即使保存失败也返回爬取的数据
        }

        return crawledScores;
    }

    /// <summary>
    /// 爬取成绩数据
    /// </summary>
    /// <param name="studentId"></param>
    /// <param name="semester"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private async Task<List<ScoreResponse>> CrawlScores(string studentId, string semester, string cookie)
    {
        var cacheKey = $"score_{studentId}_{semester}";
        if (_redis.KeyExists(cacheKey))
        {
            var result = _redis.StringGet(cacheKey);
            var cached = result.HasValue
                ? JsonConvert.DeserializeObject<List<ScoreResponse>>(result.ToString()) ?? []
                : [];
            return cached.ToList();
        }

        using var client = httpClientFactory.CreateClient();
        client.SetRealisticHeaders();
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await client.GetAsync(
            $"https://swjw.xauat.edu.cn/student/for-std/grade/sheet/info/{studentId}?semester={semester}");

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("获取成绩数据失败，HTTP状态码: {StatusCode}", response.StatusCode);
            return [];
        }

        var content = await response.Content.ReadAsStringAsync();

        // 检查返回的内容是否为HTML（表示可能需要重新登录）
        if (content.StartsWith("<"))
        {
            logger.LogWarning("获取成绩数据失败，返回了HTML内容而非JSON，可能Cookie已过期");
            return [];
        }

        JObject json;
        try
        {
            json = JObject.Parse(content);
        }
        catch (JsonReaderException ex)
        {
            logger.LogError(ex, "解析成绩JSON数据失败，原始内容: {Content}", content);
            return [];
        }

        var list = (json["semesterId2studentGrades"]?[semester]!).Select(item => new ScoreResponse()
        {
            Name = item["course"]!["nameZh"]!.ToString(),
            Credit = item["course"]!["credits"]!.ToString(),
            LessonCode = item["lessonCode"]!.ToString(),
            LessonName = item["lessonNameZh"]!.ToString(),
            Grade = item["gaGrade"]!.ToString(),
            Gpa = item["gp"]!.ToString(),
            GradeDetail = string.Join("; ",
                Regex.Matches(item["gradeDetail"]!.ToString(), @"<span[^>]*>([^<]+)<\/span>")
                    .Select(m => m.Groups[1].Value.Trim()))
        }).ToList();

        if (list.Count == 0) return list;

        SemesterItem thisSemester;
        if (!_redis.KeyExists("thisSemester"))
        {
            thisSemester = await exam.GetThisSemester(cookie);
        }
        else
        {
            var thisSemesterCache = _redis.StringGet("thisSemester");
            if (!thisSemesterCache.HasValue || thisSemesterCache.IsNullOrEmpty) return list;
            thisSemester = JsonConvert.DeserializeObject<SemesterItem>(thisSemesterCache!) ?? new SemesterItem();
        }

        if (thisSemester.Value != semester)
        {
            await _redis.StringSetAsync(cacheKey, JsonConvert.SerializeObject(list),
                expiry: new TimeSpan(5, 0, 0, 0));
        }

        return list;
    }
}