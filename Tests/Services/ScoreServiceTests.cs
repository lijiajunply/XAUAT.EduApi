using Moq;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Repos;
using EduApi.Data.Models;
using StackExchange.Redis;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// ScoreService单元测试
/// </summary>
public class ScoreServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ScoreService>> _loggerMock;
    private readonly Mock<IExamService> _examServiceMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IScoreRepository> _scoreRepositoryMock;
    private readonly ScoreService _scoreService;
    
    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public ScoreServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ScoreService>>();
        _examServiceMock = new Mock<IExamService>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _scoreRepositoryMock = new Mock<IScoreRepository>();
        
        // 模拟Redis数据库
        var redisDatabaseMock = new Mock<IDatabase>();
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 创建ScoreService实例
        _scoreService = new ScoreService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _examServiceMock.Object,
            _redisMock.Object,
            _scoreRepositoryMock.Object);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当学生ID为空时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldThrowArgumentNullException_WhenStudentIdIsEmpty()
    {
        // Arrange
        var studentId = string.Empty;
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scoreService.GetScoresAsync(studentId, semester, cookie));
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当Cookie为空时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldThrowArgumentNullException_WhenCookieIsEmpty()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2025-2026-1";
        var cookie = string.Empty;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scoreService.GetScoresAsync(studentId, semester, cookie));
    }
    
    /// <summary>
    /// 测试ParseSemesterAsync方法，验证是否正确调用了HttpClient
    /// </summary>
    [Fact]
    public async Task ParseSemesterAsync_ShouldCallHttpClient_WhenCalled()
    {
        // Arrange
        var studentId = "123456";
        var cookie = "test-cookie";
        
        // 模拟HttpClient
        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);
        
        // Act
        await _scoreService.ParseSemesterAsync(studentId, cookie);
        
        // Assert
        _httpClientFactoryMock.Verify(m => m.CreateClient(It.IsAny<string>()), Times.Once);
    }
    
    /// <summary>
    /// 测试GetThisSemesterAsync方法，验证是否正确调用了ExamService
    /// </summary>
    [Fact]
    public async Task GetThisSemesterAsync_ShouldCallExamService_WhenCalled()
    {
        // Arrange
        var cookie = "test-cookie";
        var semesterItem = new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(semesterItem);
        
        // Act
        var result = await _scoreService.GetThisSemesterAsync(cookie);
        
        // Assert
        _examServiceMock.Verify(m => m.GetThisSemester(cookie), Times.Once);
        Assert.Equal(semesterItem, result);
    }
    
    /// <summary>
    /// 测试GetScoreResponse方法，验证当为当前学期时是否直接调用CrawlScores
    /// </summary>
    [Fact]
    public async Task GetScoreResponse_ShouldCallCrawlScores_WhenCurrentSemester()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = semester, Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟CrawlScores返回空列表
        // 注意：由于CrawlScores是私有方法，我们无法直接模拟它，这里通过模拟HttpClient来间接测试
        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当学生ID为null时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldThrowArgumentNullException_WhenStudentIdIsNull()
    {
        // Arrange
        var studentId = (string)null;
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scoreService.GetScoresAsync(studentId, semester, cookie));
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当Cookie为null时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldThrowArgumentNullException_WhenCookieIsNull()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2025-2026-1";
        var cookie = (string)null;
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _scoreService.GetScoresAsync(studentId, semester, cookie));
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当学期字符串为空时是否能正常处理
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldHandleEmptySemester()
    {
        // Arrange
        var studentId = "123456";
        var semester = string.Empty;
        var cookie = "test-cookie";
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟数据库查询返回空列表
        _scoreRepositoryMock.Setup(m => m.GetByUserIdAsync(studentId)).ReturnsAsync(new List<ScoreResponse>());
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当学生ID包含特殊字符时是否能正常处理
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldHandleSpecialCharactersInStudentId()
    {
        // Arrange
        var studentId = "123456!@#$%^&*()_+";
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = semester, Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟数据库查询返回空列表
        _scoreRepositoryMock.Setup(m => m.GetByUserIdAsync(studentId)).ReturnsAsync(new List<ScoreResponse>());
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当学生ID包含多个ID时是否能正常处理
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldHandleMultipleStudentIds()
    {
        // Arrange
        var studentId = "123456,789012,345678";
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = semester, Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟数据库查询返回空列表
        _scoreRepositoryMock.Setup(m => m.GetByUserIdAsync(It.IsAny<string>())).ReturnsAsync(new List<ScoreResponse>());
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当Redis不可用时是否能正常处理
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldHandleRedisUnavailable()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // 创建一个不使用Redis的ScoreService实例
        var scoreServiceWithoutRedis = new ScoreService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _examServiceMock.Object,
            null, // Redis连接为null
            _scoreRepositoryMock.Object);
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = semester, Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟数据库查询返回空列表
        _scoreRepositoryMock.Setup(m => m.GetByUserIdAsync(studentId)).ReturnsAsync(new List<ScoreResponse>());
        
        // Act
        var result = await scoreServiceWithoutRedis.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试GetScoresAsync方法，验证当数据库返回大量数据时是否能正常处理
    /// </summary>
    [Fact]
    public async Task GetScoresAsync_ShouldHandleLargeDataFromDatabase()
    {
        // Arrange
        var studentId = "123456";
        var semester = "2025-2026-1";
        var cookie = "test-cookie";
        
        // 创建大量测试数据
        var largeScoreList = new List<ScoreResponse>();
        for (int i = 0; i < 1000; i++)
        {
            largeScoreList.Add(new ScoreResponse
            {
                Key = $"{studentId}_{semester}_CS{i.ToString("D3")}_Course{i}",
                Name = $"Course{i}",
                Credit = "2.0",
                LessonCode = $"CS{i.ToString("D3")}",
                LessonName = $"Course{i}",
                Grade = (80 + i % 20).ToString(),
                Gpa = (3.0 + (i % 20) * 0.1).ToString("F1"),
                UserId = studentId,
                Semester = semester
            });
        }
        
        // 模拟当前学期
        var currentSemester = new SemesterItem { Value = semester, Text = "2025-2026学年第一学期" };
        _examServiceMock.Setup(m => m.GetThisSemester(cookie)).ReturnsAsync(currentSemester);
        
        // 模拟数据库查询返回大量数据
        _scoreRepositoryMock.Setup(m => m.GetByUserIdAsync(studentId)).ReturnsAsync(largeScoreList);
        
        // Act
        var result = await _scoreService.GetScoresAsync(studentId, semester, cookie);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(largeScoreList.Count, result.Count);
    }
}
