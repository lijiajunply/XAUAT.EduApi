using System.Text.RegularExpressions;
using EduApi.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Repos;

namespace XAUAT.EduApi.Services;

public interface IScoreService
{
    Task<List<ScoreResponse>> GetScoresAsync(string studentId, string semester, string cookie);
    Task<SemesterResult> ParseSemesterAsync(string? studentId, string cookie);
    Task<SemesterItem> GetThisSemesterAsync(string cookie);
}

public class ScoreService : IScoreService
{
    private readonly IDatabase? _redis;
    private readonly bool _redisAvailable;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ScoreService> _logger;
    private readonly IExamService _examService;
    private readonly IScoreRepository _scoreRepository;

    public ScoreService(IHttpClientFactory httpClientFactory, ILogger<ScoreService> logger, IExamService examService,
        IConnectionMultiplexer? muxer, IScoreRepository scoreRepository)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _examService = examService;
        _scoreRepository = scoreRepository;

        // 使用扩展方法初始化Redis连接
        _redis = muxer.SafeGetDatabase(_logger, out _redisAvailable);
    }

    public async Task<List<ScoreResponse>> GetScoresAsync(string studentId, string semester, string cookie)
    {
        _logger.LogInformation("开始获取考试分数");

        if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(cookie))
        {
            throw new ArgumentNullException(nameof(studentId));
        }

        // 尝试从Redis获取缓存
        var cacheKey = CacheKeys.Scores(studentId, semester);
        var cachedScores = await _redis.GetCacheAsync<List<ScoreResponse>>(_redisAvailable, cacheKey, _logger);
        if (cachedScores is { Count: > 0 })
        {
            return cachedScores;
        }

        var split = studentId.Split(',');
        var result = new List<ScoreResponse>();

        for (var i = 0; i < split.Length; i++)
        {
            var s = split[i];
            var scoreResponse = await GetScoreResponse(s, semester, cookie).ConfigureAwait(false);
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

        // 将结果缓存到Redis
        if (result.Count > 0)
        {
            await _redis.SetCacheAsync(_redisAvailable, cacheKey, result, TimeSpan.FromHours(1), _logger);
        }

        return result;
    }

    public async Task<SemesterResult> ParseSemesterAsync(string? studentId, string cookie)
    {
        _logger.LogInformation("开始解析学期数据");

        // 尝试从Redis获取缓存
        var cacheKey = CacheKeys.SemesterResult(studentId);
        var cachedResult = await _redis.GetCacheAsync<SemesterResult>(_redisAvailable, cacheKey, _logger);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        using var client = _httpClientFactory.CreateClient();
        client.ConfigureForEduSystem(cookie, TimeSpan.FromSeconds(3));

        var url = "https://swjw.xauat.edu.cn/student/for-std/grade/sheet";
        if (!string.IsNullOrEmpty(studentId))
        {
            var split = studentId.Split(',');
            studentId = split.FirstOrDefault();
            url += $"/semester-index/{studentId}";
        }

        var html = await client.GetStringAsync(url).ConfigureAwait(false);

        if (html.Contains("登入页面"))
        {
            throw new Exceptions.UnAuthenticationError();
        }

        var result = new SemesterResult();
        result.Parse(html);

        // 将结果缓存到Redis
        await _redis.SetCacheAsync(_redisAvailable, cacheKey, result, TimeSpan.FromHours(1), _logger);

        return result;
    }

    public async Task<SemesterItem> GetThisSemesterAsync(string cookie)
    {
        return await _examService.GetThisSemester(cookie);
    }

    private async Task<IEnumerable<ScoreResponse>> GetScoreResponse(string studentId, string semester, string cookie)
    {
        // 判断是否为当前学期
        var thisSemester = await _examService.GetThisSemester(cookie).ConfigureAwait(false);
        var isCurrentSemester = thisSemester != null! && thisSemester.Value == semester;

        if (!isCurrentSemester && thisSemester != null)
        {
            if (int.TryParse(semester, out var theSemesterInt) && int.TryParse(thisSemester.Value, out var thisSemesterInt))
            {
                isCurrentSemester = Math.Abs(thisSemesterInt - theSemesterInt) <= 60;
            }
        }

        if (!isCurrentSemester)
        {
            // 从数据库查询
            var dbScores = await _scoreRepository.GetByUserIdAsync(studentId).ConfigureAwait(false);
            dbScores = dbScores.Where(s => s.Semester == semester);

            // 如果数据库中有数据，直接返回
            var enumerable = dbScores as ScoreResponse[] ?? dbScores.ToArray();
            var scoreResponses = dbScores as ScoreResponse[] ?? enumerable.ToArray();
            if (scoreResponses.Length != 0)
            {
                return scoreResponses;
            }
        }

        var crawledScores = await CrawlScores(studentId, semester, cookie).ConfigureAwait(false);
        var scoresToSave = crawledScores.Select(score =>
        {
            // 为每个成绩项生成唯一键值
            score.Key = $"{studentId}_{semester}_{score.LessonCode}_{score.Name}".GetHashCode().ToString();
            // 设置外键关联
            score.UserId = studentId;
            score.Semester = semester;
            return score;
        }).ToList();

        if (isCurrentSemester)
        {
            return crawledScores;
        }

        try
        {
            await _scoreRepository.AddRangeAsync(scoresToSave).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存成绩数据到数据库时出错");
            // 即使保存失败也返回爬取的数据
        }

        return crawledScores;
    }

    private async Task<List<ScoreResponse>> CrawlScores(string studentId, string semester, string cookie)
    {
        using var client = _httpClientFactory.CreateClient();
        client.ConfigureForEduSystem(cookie);

        var response = await client.GetAsync(
                $"https://swjw.xauat.edu.cn/student/for-std/grade/sheet/info/{studentId}?semester={semester}")
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("获取成绩数据失败，HTTP状态码: {StatusCode}", response.StatusCode);
            return [];
        }

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // 检查返回的内容是否为HTML（表示可能需要重新登录）
        if (content.StartsWith('<'))
        {
            _logger.LogWarning("获取成绩数据失败，返回了HTML内容而非JSON，可能Cookie已过期");
            if (content.Contains("登入页面"))
            {
                throw new Exceptions.UnAuthenticationError();
            }

            return [];
        }

        JObject json;
        try
        {
            json = JObject.Parse(content);
        }
        catch (JsonReaderException ex)
        {
            _logger.LogError(ex, "解析成绩JSON数据失败，原始内容: {Content}", content);
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

        // 缓存当前学期信息
        var thisSemesterCache = await _redis.GetStringCacheAsync(_redisAvailable, CacheKeys.ThisSemester, _logger);
        if (string.IsNullOrEmpty(thisSemesterCache))
        {
            await _examService.GetThisSemester(cookie).ConfigureAwait(false);
        }

        return list;
    }
}
