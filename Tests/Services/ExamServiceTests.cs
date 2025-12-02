using Moq;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Services;
using EduApi.Data.Models;
using StackExchange.Redis;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// ExamService单元测试
/// </summary>
public class ExamServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ExamService>> _loggerMock;
    private readonly Mock<IInfoService> _infoServiceMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly ExamService _examService;
    
    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public ExamServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ExamService>>();
        _infoServiceMock = new Mock<IInfoService>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        
        // 模拟Redis数据库
        var redisDatabaseMock = new Mock<IDatabase>();
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 创建ExamService实例
        _examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);
    }
    
    /// <summary>
    /// 测试GetThisSemester方法，验证当Redis缓存存在时是否直接返回缓存数据
    /// </summary>
    [Fact]
    public async Task GetThisSemester_ShouldReturnCachedData_WhenRedisCacheExists()
    {
        // Arrange
        var cookie = "test-cookie";
        var expectedSemester = new SemesterItem { Value = "2025-2026-1", Text = "2025-2026学年第一学期" };
        var serializedSemester = Newtonsoft.Json.JsonConvert.SerializeObject(expectedSemester);
        
        // 模拟Redis数据库
        var redisDatabaseMock = new Mock<IDatabase>();
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 模拟Redis缓存
        redisDatabaseMock.Setup(m => m.StringGetAsync("thisSemester", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedSemester));
        
        // 创建ExamService实例
        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);
        
        // Act
        var result = await examService.GetThisSemester(cookie);
        
        // Assert
        Assert.Equal(expectedSemester.ToString(), result.ToString());
        Assert.Equal(expectedSemester.Value, result.Value);
        Assert.Equal(expectedSemester.Text, result.Text);
    }
    
    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证当ID为空时是否调用GetExamArrangementAsync
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldCallGetExamArrangementAsync_WhenIdIsEmpty()
    {
        // Arrange
        var cookie = "test-cookie";
        string? id = null;
        
        // 模拟HttpClient
        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);
        
        // Act
        var result = await _examService.GetExamArrangementsAsync(cookie, id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Exams);
    }
    
    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证当ID包含逗号时是否拆分ID并多次调用GetExamArrangementAsync
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldSplitId_WhenIdContainsComma()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123,456";
        
        // 模拟HttpClient
        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);
        
        // Act
        var result = await _examService.GetExamArrangementsAsync(cookie, id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Exams);
    }
}
