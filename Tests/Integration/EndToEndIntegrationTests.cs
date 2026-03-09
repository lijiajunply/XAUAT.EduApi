using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using StackExchange.Redis;
using System.Net;
using EduApi.Data;
using EduApi.Data.Models;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Integration;

/// <summary>
/// 端到端集成测试
/// 测试完整的业务流程，从控制器到服务到数据访问层
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EduContext _dbContext;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public EndToEndIntegrationTests()
    {
        var services = new ServiceCollection();

        // 配置内存数据库
        services.AddDbContext<EduContext>(options =>
            options.UseInMemoryDatabase(databaseName: $"EndToEndTestDb_{Guid.NewGuid()}"));

        // 配置Redis Mock
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);
        services.AddSingleton(_redisMock.Object);

        // 配置HttpClientFactory Mock
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        services.AddSingleton(_httpClientFactoryMock.Object);

        // 配置日志
        services.AddLogging(builder => builder.AddConsole());

        // 配置缓存服务
        var cacheOptions = new CacheOptions
        {
            DefaultExpiration = TimeSpan.FromHours(1),
            StrategyType = CacheStrategyType.Hybrid,
            LocalCacheMaxSize = 1000
        };
        services.AddSingleton(Options.Create(cacheOptions));
        services.AddSingleton<ICacheService, CacheService>();

        // 配置仓库
        services.AddScoped<IScoreRepository, ScoreRepository>();

        // 配置服务
        services.AddScoped<IInfoService, InfoService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IScoreService, ScoreService>();
        services.AddScoped<ICourseService, CourseService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<EduContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }

    #region Score Service Integration Tests

    [Fact]
    public async Task ScoreService_ShouldReturnCachedScores_WhenCacheHit()
    {
        // Arrange
        var scoreService = _serviceProvider.GetRequiredService<IScoreService>();
        var studentId = "2021001";
        var semester = "301";
        var cookie = "test-cookie";

        var cachedScores = new List<ScoreResponse>
        {
            new() { Name = "高等数学", Grade = "90", Credit = "4", Gpa = "4.0" }
        };
        var cachedJson = Newtonsoft.Json.JsonConvert.SerializeObject(cachedScores);

        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedJson));

        // Act
        var result = await scoreService.GetScoresAsync(studentId, semester, cookie);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("高等数学", result[0].Name);
    }

    [Fact]
    public async Task ScoreService_ShouldFetchFromDatabase_WhenNotCurrentSemester()
    {
        // Arrange
        var scoreService = _serviceProvider.GetRequiredService<IScoreService>();
        var studentId = "2021001";
        var semester = "200"; // 非当前学期
        var cookie = "test-cookie";

        // 设置Redis返回空
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // 设置当前学期
        var currentSemester = new SemesterItem { Value = "301", Text = "2025-2026-1" };
        var semesterJson = Newtonsoft.Json.JsonConvert.SerializeObject(currentSemester);
        _redisDatabaseMock.Setup(x => x.StringGetAsync("eduapi:thisSemester", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(semesterJson));

        // 添加测试数据到数据库
        _dbContext.Scores.Add(new ScoreResponse
        {
            Key = "test-key",
            UserId = studentId,
            Semester = semester,
            Name = "线性代数",
            Grade = "85",
            Credit = "3",
            Gpa = "3.5"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await scoreService.GetScoresAsync(studentId, semester, cookie);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("线性代数", result[0].Name);
    }

    #endregion

    #region Cache Service Integration Tests

    [Fact]
    public async Task CacheService_ShouldWorkWithMultipleLevels()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        var key = "integration-test-key";
        var value = "integration-test-value";

        // Act - 设置本地缓存
        await cacheService.SetAsync(key, value, level: CacheLevel.Local);
        var localResult = await cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal(value, localResult);
    }

    [Fact]
    public async Task CacheService_GetOrCreate_ShouldCacheFactoryResult()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        var key = "factory-test-key";
        var factoryCallCount = 0;

        // Act
        var result1 = await cacheService.GetOrCreateAsync(key, async () =>
        {
            factoryCallCount++;
            return await Task.FromResult("factory-value");
        }, level: CacheLevel.Local);

        var result2 = await cacheService.GetOrCreateAsync(key, async () =>
        {
            factoryCallCount++;
            return await Task.FromResult("factory-value-2");
        }, level: CacheLevel.Local);

        // Assert
        Assert.Equal("factory-value", result1);
        Assert.Equal("factory-value", result2);
        Assert.Equal(1, factoryCallCount); // 工厂方法只应该被调用一次
    }

    [Fact]
    public async Task CacheService_BatchOperations_ShouldWork()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        var items = new Dictionary<string, string?>
        {
            { "batch-key-1", "batch-value-1" },
            { "batch-key-2", "batch-value-2" },
            { "batch-key-3", "batch-value-3" }
        };

        // Act
        await cacheService.SetManyAsync(items, level: CacheLevel.Local);
        var results = await cacheService.GetManyAsync<string>(items.Keys, CacheLevel.Local);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("batch-value-1", results["batch-key-1"]);
        Assert.Equal("batch-value-2", results["batch-key-2"]);
        Assert.Equal("batch-value-3", results["batch-key-3"]);
    }

    #endregion

    #region Exam Service Integration Tests

    [Fact]
    public async Task ExamService_ShouldReturnCachedExams_WhenCacheHit()
    {
        // Arrange
        var examService = _serviceProvider.GetRequiredService<IExamService>();
        var cookie = "test-cookie";
        var id = "2021001";

        var cachedExam = new ExamResponse
        {
            Exams = new List<ExamInfo>
            {
                new() { Name = "高等数学期末考试", Time = "2025-01-15 09:00", Location = "教学楼A101", Seat = "15" }
            },
            CanClick = true
        };
        var cachedJson = Newtonsoft.Json.JsonConvert.SerializeObject(cachedExam);

        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedJson));

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Exams);
        Assert.Equal("高等数学期末考试", result.Exams[0].Name);
    }

    [Fact]
    public async Task ExamService_ShouldHandleMultipleStudentIds()
    {
        // Arrange
        var examService = _serviceProvider.GetRequiredService<IExamService>();
        var cookie = "test-cookie";
        var id = "2021001,2021002";

        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>var studentExamInfoVms = [];</html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Full Flow Integration Tests

    [Fact]
    public async Task FullFlow_StudentDataRetrieval_ShouldWork()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        var studentId = "2021001";

        // 模拟学生数据缓存
        var studentData = new
        {
            Id = studentId,
            Name = "张三",
            Major = "计算机科学与技术"
        };

        // Act - 缓存学生数据
        await cacheService.SetAsync($"student:{studentId}", studentData, level: CacheLevel.Local);

        // Act - 获取学生数据
        var cachedData = await cacheService.GetAsync<object>($"student:{studentId}");

        // Assert
        Assert.NotNull(cachedData);
    }

    [Fact]
    public async Task FullFlow_CacheWarmup_ShouldPreloadData()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();

        // 添加预热任务
        cacheService.AddWarmupTask(new CacheWarmupItem
        {
            Key = "warmup:semester",
            ValueFactory = async () => await Task.FromResult(new SemesterItem { Value = "301", Text = "2025-2026-1" }),
            Priority = 10
        });

        cacheService.AddWarmupTask(new CacheWarmupItem
        {
            Key = "warmup:config",
            ValueFactory = async () => await Task.FromResult(new { MaxRetries = 3, Timeout = 5000 }),
            Priority = 5
        });

        // Act
        var completedTasks = await cacheService.ExecuteWarmupAsync();

        // Assert
        Assert.Equal(2, completedTasks);

        // 验证数据已被缓存
        var semester = await cacheService.GetAsync<SemesterItem>("warmup:semester");
        Assert.NotNull(semester);
        Assert.Equal("301", semester.Value);
    }

    #endregion

    #region Error Handling Integration Tests

    [Fact]
    public async Task Integration_ShouldHandleRedisFailure_Gracefully()
    {
        // Arrange
        var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        var key = "redis-failure-test";

        // 模拟Redis失败
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        // Act - 应该降级到本地缓存
        await cacheService.SetAsync(key, "test-value", level: CacheLevel.Local);
        var result = await cacheService.GetAsync<string>(key);

        // Assert
        Assert.Equal("test-value", result);
    }

    [Fact]
    public async Task Integration_ShouldHandleDatabaseFailure_Gracefully()
    {
        // Arrange
        var scoreService = _serviceProvider.GetRequiredService<IScoreService>();
        var studentId = "invalid-student";
        var semester = "301";
        var cookie = "test-cookie";

        // 设置Redis返回空
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // 设置当前学期
        var currentSemester = new SemesterItem { Value = "301", Text = "2025-2026-1" };
        var semesterJson = Newtonsoft.Json.JsonConvert.SerializeObject(currentSemester);
        _redisDatabaseMock.Setup(x => x.StringGetAsync("eduapi:thisSemester", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(semesterJson));

        // 模拟HTTP请求失败
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await scoreService.GetScoresAsync(studentId, semester, cookie);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion
}
