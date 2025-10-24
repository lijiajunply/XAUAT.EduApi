using System.Text.RegularExpressions;
using EduApi.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Services;

public class ScoreService(IHttpClientFactory httpClientFactory, ILogger<ScoreService> logger, IExamService examService, IConnectionMultiplexer muxer, IScoreRepository scoreRepository) : IScoreService
{
    private readonly IDatabase _redis = muxer.GetDatabase();

    public async Task<List<ScoreResponse>> GetScoresAsync(string studentId, string semester, string cookie)
    {
        logger.LogInformation("开始获取考试分数");
        
        if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(cookie))
        {
            throw new ArgumentNullException(nameof(studentId));
        }

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

    public async Task<SemesterResult> ParseSemesterAsync(string? studentId, string cookie)
    {
        logger.LogInformation("开始解析学期数据");
        
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
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

    public async Task<SemesterItem> GetThisSemesterAsync(string cookie)
    {
        return await examService.GetThisSemester(cookie);
    }

    private async Task<IEnumerable<ScoreResponse>> GetScoreResponse(string studentId, string semester, string cookie)
    {
        // 先判断是否为当前学期
        var thisSemester = await examService.GetThisSemester(cookie);
        var isCurrentSemester = thisSemester.Value == semester;

        // 如果是当前学期，直接爬虫
        if (isCurrentSemester)
        {
            return await CrawlScores(studentId, semester, cookie);
        }

        // 如果不是当前学期，先从数据库查询
        var dbScores = await scoreRepository.GetByUserIdAsync(studentId);
        dbScores = dbScores.Where(s => s.Semester == semester);

        // 如果数据库中有数据，直接返回
        var scoreResponses = dbScores as ScoreResponse[] ?? dbScores.ToArray();
        if (scoreResponses.Length != 0)
        {
            return scoreResponses;
        }

        // 如果数据库中没有数据，则爬虫获取并保存到数据库
        var crawledScores = await CrawlScores(studentId, semester, cookie);
        var scoresToSave = crawledScores.Select(score =>
        {
            // 为每个成绩项生成唯一键值
            score.Key = $"{studentId}_{semester}_{score.LessonCode}_{score.Name}".GetHashCode().ToString();
            // 设置外键关联
            score.UserId = studentId;
            score.Semester = semester;
            return score;
        }).ToList();

        try
        {
            await scoreRepository.AddRangeAsync(scoresToSave);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "保存成绩数据到数据库时出错");
            // 即使保存失败也返回爬取的数据
        }

        return crawledScores;
    }

    private async Task<List<ScoreResponse>> CrawlScores(string studentId, string semester, string cookie)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
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
        if (content.StartsWith('<'))
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

        if (!_redis.KeyExists("thisSemester"))
        {
            await examService.GetThisSemester(cookie);
        }
        else
        {
            var thisSemesterCache = _redis.StringGet("thisSemester");
            if (!thisSemesterCache.HasValue || thisSemesterCache.IsNullOrEmpty) return list;
            _ = JsonConvert.DeserializeObject<SemesterItem>(thisSemesterCache!) ?? new SemesterItem();
        }

        return list;
    }
}