using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Services;
using EduApi.Data.Models;
using StackExchange.Redis;
using System.Net;
using Newtonsoft.Json;

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
    private readonly Mock<IDatabase> _redisDatabaseMock;
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
        _redisDatabaseMock = new Mock<IDatabase>();

        // 模拟Redis数据库
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDatabaseMock.Object);

        // 创建ExamService实例
        _examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);
    }

    #region GetThisSemester Tests

    /// <summary>
    /// 测试GetThisSemester方法，验证当Redis缓存存在时是否直接返回缓存数据
    /// </summary>
    [Fact]
    public async Task GetThisSemester_ShouldReturnCachedData_WhenRedisCacheExists()
    {
        // Arrange
        var cookie = "test-cookie";
        var expectedSemester = new SemesterItem { Value = "301", Text = "2025-2026-1" };
        var serializedSemester = JsonConvert.SerializeObject(expectedSemester);

        // 模拟Redis缓存
        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedSemester));

        // 创建新的ExamService实例以使用新的Redis设置
        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act
        var result = await examService.GetThisSemester(cookie);

        // Assert
        Assert.Equal(expectedSemester.Value, result.Value);
        Assert.Equal(expectedSemester.Text, result.Text);
    }

    /// <summary>
    /// 测试GetThisSemester方法，验证当Redis缓存为空时是否从HTTP获取数据
    /// </summary>
    [Fact]
    public async Task GetThisSemester_ShouldFetchFromHttp_WhenCacheMiss()
    {
        // Arrange
        var cookie = "test-cookie";
        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _infoServiceMock.Setup(m => m.IsGreatThanStart()).Returns(true);

        var htmlContent = @"
            <html>
                <body>
                    <div>课表</div>
                    <select>
                        <option selected=""selected"" value=""301"">2025-2026-1</option>
                    </select>
                </body>
            </html>";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act
        var result = await examService.GetThisSemester(cookie);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("301", result.Value);
    }

    /// <summary>
    /// 测试GetThisSemester方法，验证当返回登录页面时是否抛出UnAuthenticationError
    /// </summary>
    [Fact]
    public async Task GetThisSemester_ShouldThrowUnAuthenticationError_WhenSessionExpired()
    {
        // Arrange
        var cookie = "expired-cookie";
        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>登入页面</html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<XAUAT.EduApi.Exceptions.UnAuthenticationError>(() => examService.GetThisSemester(cookie));
    }

    #endregion

    #region GetExamArrangementsAsync Tests

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

    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证当ID不包含逗号时是否直接调用GetExamArrangementAsync
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldNotSplitId_WhenIdDoesNotContainComma()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123";

        // 模拟HttpClient
        var httpClientMock = new Mock<HttpClient>();
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

        // Act
        var result = await _examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证当Redis缓存存在时是否直接返回缓存数据
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldReturnCachedData_WhenRedisCacheExists()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123";
        var expectedExam = new ExamResponse
        {
            Exams = new List<ExamInfo>
            {
                new ExamInfo { Name = "高等数学", Time = "2025-01-15 09:00", Location = "教学楼A101", Seat = "15" }
            },
            CanClick = true
        };
        var serializedExam = JsonConvert.SerializeObject(expectedExam);

        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(serializedExam));

        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Exams);
        Assert.Equal("高等数学", result.Exams[0].Name);
    }

    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证HTTP请求失败时的错误处理
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldHandleHttpError()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123";

        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Exams);
        Assert.False(result.CanClick);
    }

    /// <summary>
    /// 测试GetExamArrangementsAsync方法，验证并行获取多个学生考试安排
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldFetchInParallel_WhenMultipleIds()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123,456,789";

        _redisDatabaseMock.Setup(m => m.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
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

        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            _redisMock.Object);

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// 测试当Redis不可用时的降级处理
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldWorkWithoutRedis()
    {
        // Arrange
        var cookie = "test-cookie";
        var id = "123";

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

        // 创建没有Redis的ExamService
        var examService = new ExamService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _infoServiceMock.Object,
            null);

        // Act
        var result = await examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试空Cookie的处理
    /// </summary>
    [Fact]
    public async Task GetExamArrangementsAsync_ShouldHandleEmptyCookie()
    {
        // Arrange
        var cookie = string.Empty;
        var id = "123";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>登入页面</html>")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _examService.GetExamArrangementsAsync(cookie, id);

        // Assert
        Assert.NotNull(result);
    }

    #endregion
}
