using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Caching;
using EduApi.Data;
using EduApi.Data.Models;
using Moq.Protected;

namespace XAUAT.EduApi.Tests.Integration;

/// <summary>
/// ScoreService集成测试
/// </summary>
public class ScoreServiceIntegrationTests : IDisposable
{
    private readonly DbContextOptions<EduContext> _dbContextOptions;
    private readonly EduContext _dbContext;
    private readonly ScoreService _scoreService;
    private readonly ScoreRepository _scoreRepository;
    
    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public ScoreServiceIntegrationTests()
    {
        // 使用内存数据库进行集成测试
        _dbContextOptions = new DbContextOptionsBuilder<EduContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        
        // 创建数据库上下文
        _dbContext = new EduContext(_dbContextOptions);
        
        // 初始化数据库
        _dbContext.Database.EnsureCreated();
        
        // 创建依赖项 - 使用HttpMessageHandler模拟HTTP响应
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NotFound
            });
        httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handlerMock.Object, disposeHandler: false));
        var httpClientFactory = httpClientFactoryMock.Object;
        var logger = new Mock<ILogger<ScoreService>>().Object;
        var examServiceMock = new Mock<IExamService>();
        // 设置 GetThisSemester 方法，防止返回 null
        examServiceMock.Setup(m => m.GetThisSemester(It.IsAny<string>()))
            .ReturnsAsync(new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" });
        var examService = examServiceMock.Object;
        var cacheServiceMock = new Mock<ICacheService>();
        // 设置GetOrCreateAsync透传，确保工厂方法能正常执行
        cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ScoreResponse>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<Task<List<ScoreResponse>>> factory, TimeSpan? exp, CacheLevel level, int priority, CancellationToken ct) =>
                await factory());

        // 创建仓库和服务
        var dbContextFactory = new TestDbContextFactory(_dbContextOptions);
        _scoreRepository = new ScoreRepository(dbContextFactory);
        _scoreService = new ScoreService(
            httpClientFactory,
            logger,
            examService,
            cacheServiceMock.Object,
            _scoreRepository);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证从数据库获取成绩数据
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldReturnDataFromDatabase_WhenDataExists()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2024-2025-2";
        var cookie = "test-cookie";
        
        // 添加测试数据到数据库
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
        
        // 模拟当前学期
        var examServiceMock = new Mock<IExamService>();
        examServiceMock.Setup(m => m.GetThisSemester(cookie))
            .ReturnsAsync(new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" });
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Name == "计算机基础" && s.Grade == "85");
        Assert.Contains(result, s => s.Name == "高等数学" && s.Grade == "90");
    }
    
    /// <summary>
    /// 测试GetScoreResponse方法，验证当数据库中没有数据时是否返回空列表
    /// </summary>
    [Fact]
    public async Task GetScoreResponse_ShouldReturnEmptyList_WhenNoDataInDatabase()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2024-2025-1";
        var cookie = "test-cookie";
        
        // 模拟当前学期
        var examServiceMock = new Mock<IExamService>();
        examServiceMock.Setup(m => m.GetThisSemester(cookie))
            .ReturnsAsync(new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" });
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 清理测试资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

internal class TestDbContextFactory(DbContextOptions<EduContext> options) : IDbContextFactory<EduContext>
{
    public EduContext CreateDbContext() => new EduContext(options);
}
