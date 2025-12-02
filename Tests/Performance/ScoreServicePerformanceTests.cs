using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Repos;
using EduApi.Data;
using EduApi.Data.Models;

namespace XAUAT.EduApi.Tests.Performance;

/// <summary>
/// ScoreService性能测试
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ScoreServicePerformanceTests
{
    private DbContextOptions<EduContext> _dbContextOptions;
    private EduContext _dbContext;
    private ScoreService _scoreService;
    private const string StudentId = "123456";
    private const string Semester = "2025-2026-1";
    private const string Cookie = "test-cookie";

    /// <summary>
    /// 性能测试前的初始化设置
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        // 使用内存数据库进行性能测试
        _dbContextOptions = new DbContextOptionsBuilder<EduContext>()
            .UseInMemoryDatabase(databaseName: "PerformanceTestDatabase")
            .Options;

        // 创建数据库上下文
        _dbContext = new EduContext(_dbContextOptions);

        // 初始化数据库
        _dbContext.Database.EnsureCreated();

        // 添加测试数据
        var testScores = new List<ScoreResponse>();
        for (int i = 0; i < 100; i++)
        {
            testScores.Add(new ScoreResponse
            {
                Key = $"{StudentId}_{Semester}_CS{i.ToString("D3")}_Course{i}",
                Name = $"Course{i}",
                Credit = "2.0",
                LessonCode = $"CS{i.ToString("D3")}",
                LessonName = $"Course{i}",
                Grade = (80 + i % 20).ToString(),
                Gpa = (3.0 + (i % 20) * 0.1).ToString("F1"),
                UserId = StudentId,
                Semester = Semester
            });
        }

        _dbContext.Scores.AddRange(testScores);
        _dbContext.SaveChanges();

        // 创建服务依赖
        var httpClientFactory = new Mock<IHttpClientFactory>().Object;
        var logger = new Mock<ILogger<ScoreService>>().Object;
        var examService = new Mock<IExamService>();
        examService.Setup(m => m.GetThisSemester(It.IsAny<string>()))
            .ReturnsAsync(new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" });
        var redisConnection = new Mock<IConnectionMultiplexer>().Object;

        // 创建仓库和服务
        var scoreRepository = new ScoreRepository(_dbContext);
        _scoreService = new ScoreService(
            httpClientFactory,
            logger,
            examService.Object,
            redisConnection,
            scoreRepository);
    }

    /// <summary>
    /// 性能测试后的清理工作
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// 测试GetScoresAsync方法的性能
    /// </summary>
    [Benchmark]
    [Arguments(100)]
    public async Task GetScoresAsync_PerformanceTest(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            await _scoreService.GetScoresAsync(StudentId, Semester, Cookie);
        }
    }

    /// <summary>
    /// 测试不同学生ID的成绩查询性能
    /// </summary>
    [Benchmark]
    public async Task GetScoresAsync_DifferentStudentIds()
    {
        // 测试不同学生的成绩查询
        for (int i = 0; i < 50; i++)
        {
            var studentId = $"{100000 + i}";
            await _scoreService.GetScoresAsync(studentId, Semester, Cookie);
        }
    }
}
