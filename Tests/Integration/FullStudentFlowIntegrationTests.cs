using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Caching;

namespace XAUAT.EduApi.Tests.Integration;

/// <summary>
/// 完整学生业务流程集成测试
/// </summary>
public class FullStudentFlowIntegrationTests : IDisposable
{
    private readonly DbContextOptions<EduContext> _dbContextOptions;
    private readonly EduContext _dbContext;
    private readonly ScoreService _scoreService;
    private readonly CourseService _courseService;
    private readonly PaymentService _paymentService;
    private readonly InfoService _infoService;
    private readonly ScoreRepository _scoreRepository;
    private readonly Mock<IExamService> _examServiceMock;
    private readonly Mock<IConnectionMultiplexer> _redisConnectionMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public FullStudentFlowIntegrationTests()
    {
        // 为每个测试创建唯一的数据库名称，确保测试隔离
        var uniqueDatabaseName = "TestDatabase_FullStudentFlow_" + Guid.NewGuid();

        // 使用内存数据库进行集成测试
        _dbContextOptions = new DbContextOptionsBuilder<EduContext>()
            .UseInMemoryDatabase(databaseName: uniqueDatabaseName)
            .Options;

        // 创建数据库上下文
        _dbContext = new EduContext(_dbContextOptions);

        // 初始化数据库
        _dbContext.Database.EnsureCreated();

        // 创建Redis模拟
        _redisConnectionMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _redisConnectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        // 创建ExamService模拟
        _examServiceMock = new Mock<IExamService>();
        _examServiceMock.Setup(m => m.GetThisSemester(It.IsAny<string>()))
            .ReturnsAsync(new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" });

        // 创建HttpClientFactory模拟
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        // 创建CacheService模拟
        _cacheServiceMock = new Mock<ICacheService>();
        _cacheServiceMock.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<CourseActivity>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<List<CourseActivity>>>, TimeSpan?, CacheLevel, int, CancellationToken>(async (
                key, factory, expiration, level, priority, token) => await factory());

        // 创建日志模拟
        var courseLogger = new Mock<ILogger<CourseService>>().Object;
        var scoreLogger = new Mock<ILogger<ScoreService>>().Object;
        var paymentLogger = new Mock<ILogger<PaymentService>>().Object;

        // 创建成绩仓库和服务
        _scoreRepository = new ScoreRepository(_dbContext);
        _scoreService = new ScoreService(
            _httpClientFactoryMock.Object,
            scoreLogger,
            _examServiceMock.Object,
            _redisConnectionMock.Object,
            _scoreRepository);

        // 创建信息服务
        _infoService = new InfoService();

        // 创建课程服务
        _courseService = new CourseService(
            _httpClientFactoryMock.Object,
            courseLogger,
            _examServiceMock.Object,
            _cacheServiceMock.Object, _infoService);

        // 创建支付服务
        _paymentService = new PaymentService(
            _redisConnectionMock.Object,
            _httpClientFactoryMock.Object,
            paymentLogger);
    }

    /// <summary>
    /// 测试完整学生业务流程：成绩查询 + 课程查询 + 支付查询 + 时间信息
    /// </summary>
    [Fact]
    public async Task FullStudentFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var studentId = "123456";
        var cardNum = "123456";
        var cookie = "test-cookie";
        var semester = "2025-2026-1";

        // 添加测试成绩数据到数据库
        var testScores = new List<ScoreResponse>
        {
            new ScoreResponse
            {
                Key = $"{studentId}_{semester}_CS101_计算机基础",
                Name = "计算机基础",
                Credit = "2.0",
                LessonCode = "CS101",
                LessonName = "计算机基础",
                Grade = "85",
                Gpa = "3.5",
                GradeDetail = "平时成绩: 90; 期末成绩: 80",
                UserId = studentId,
                Semester = semester
            },
            new ScoreResponse
            {
                Key = $"{studentId}_{semester}_MA101_高等数学",
                Name = "高等数学",
                Credit = "4.0",
                LessonCode = "MA101",
                LessonName = "高等数学",
                Grade = "90",
                Gpa = "4.0",
                GradeDetail = "平时成绩: 95; 期末成绩: 85",
                UserId = studentId,
                Semester = semester
            }
        };

        await _dbContext.Scores.AddRangeAsync(testScores);
        await _dbContext.SaveChangesAsync();

        // 设置Redis返回模拟的支付token
        var expectedToken = "test-payment-token";
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedToken);

        // Act - 获取时间信息
        var timeInfo = _infoService.GetTime();
        var isInSchool = _infoService.IsGreatThanStart();

        // Act - 获取成绩数据
        var scoresResult = await _scoreService.GetScoresAsync(studentId, semester, cookie);

        // Act - 获取支付数据
        var paymentToken = await _paymentService.Login(cardNum);

        // Assert
        Assert.NotNull(timeInfo);
        Assert.NotNull(timeInfo.StartTime);
        Assert.NotNull(timeInfo.EndTime);
        Assert.IsType<bool>(isInSchool);

        Assert.NotNull(scoresResult);
        Assert.Equal(2, scoresResult.Count);
        Assert.Equal("计算机基础", scoresResult[0].Name);
        Assert.Equal("高等数学", scoresResult[1].Name);
        Assert.Equal("85", scoresResult[0].Grade);
        Assert.Equal("90", scoresResult[1].Grade);

        Assert.NotNull(paymentToken);
        Assert.Equal(expectedToken, paymentToken);
    }

    /// <summary>
    /// 测试多个服务在异常情况下的交互
    /// </summary>
    [Fact]
    public async Task MultipleServices_ShouldHandleExceptionsCorrectly()
    {
        // Arrange
        var studentId = "123456";
        var cardNum = "123456";
        var cookie = "test-cookie";
        var semester = "2025-2026-1";

        // 设置Redis操作失败
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new Exception("Redis operation timed out"));

        // Act & Assert
        // 验证成绩服务在Redis失败时仍能从数据库获取数据
        var scoresResult = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        Assert.NotNull(scoresResult);

        // 验证支付服务在Redis失败时能正确处理异常
        await Assert.ThrowsAsync<PaymentServiceException>(() =>
            _paymentService.Login(cardNum));

        // 验证课程服务在HttpClient调用失败时能正确处理异常
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));

        // 验证信息服务不受其他服务异常影响
        var timeInfo = _infoService.GetTime();
        Assert.NotNull(timeInfo);
    }

    /// <summary>
    /// 测试环境变量对InfoService的影响以及与其他服务的集成
    /// </summary>
    [Fact]
    public void EnvironmentVariables_ShouldAffectInfoService()
    {
        // Arrange
        const string expectedStartTime = "2025-01-01";
        const string expectedEndTime = "2025-12-31";

        // 设置环境变量
        Environment.SetEnvironmentVariable("START", expectedStartTime, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", expectedEndTime, EnvironmentVariableTarget.Process);

        // Act
        var result = _infoService.GetTime();
        var isInSchool = _infoService.IsGreatThanStart();

        // Assert
        Assert.Equal(expectedStartTime, result.StartTime);
        Assert.Equal(expectedEndTime, result.EndTime);
        Assert.IsType<bool>(isInSchool);

        // 清理环境变量
        Environment.SetEnvironmentVariable("START", null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", null, EnvironmentVariableTarget.Process);
    }

    /// <summary>
    /// 清理测试资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}