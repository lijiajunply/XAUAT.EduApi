using EduApi.Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Caching;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// CourseService单元测试
/// </summary>
public class CourseServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<CourseService>> _loggerMock;
    private readonly Mock<IExamService> _examServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly CourseService _courseService;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public CourseServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CourseService>>();
        _examServiceMock = new Mock<IExamService>();
        _cacheServiceMock = new Mock<ICacheService>();

        // Mock CacheService.GetOrCreateAsync to simply execute the factory
        _cacheServiceMock.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<CourseActivity>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<List<CourseActivity>>>, TimeSpan?, CacheLevel, int, CancellationToken>(async (
                key, factory, expiration, level, priority, token) => await factory());

        // 设置HttpClientFactory返回一个有效的HttpClient实例
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        var infoService = new InfoService();
        _courseService = new CourseService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _examServiceMock.Object,
            _cacheServiceMock.Object,
            infoService);
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当studentId为空时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowException_WhenStudentIdIsEmpty()
    {
        // Arrange
        var studentId = string.Empty;
        var cookie = "test-cookie";

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当cookie为空时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowException_WhenCookieIsEmpty()
    {
        // Arrange
        var studentId = "123456";
        var cookie = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当无法获取学期信息时是否抛出InvalidOperationException
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowInvalidOperationException_WhenSemesterIsEmpty()
    {
        // Arrange
        var studentId = "123456";
        var cookie = "test-cookie";

        // 设置ExamService返回空学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(new SemesterItem { Value = string.Empty, Text = string.Empty });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当studentId为null时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowException_WhenStudentIdIsNull()
    {
        // Arrange
        string? studentId = null;
        var cookie = "test-cookie";

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当cookie为null时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowException_WhenCookieIsNull()
    {
        // Arrange
        var studentId = "123456";
        string? cookie = null;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当studentId包含多个值时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldHandleMultipleStudentIds()
    {
        // Arrange
        var studentId = "123456,789012";
        var cookie = "test-cookie";
        var semester = new SemesterItem { Value = "2024-2025-1", Text = "2024-2025学年第一学期" };

        // 设置ExamService返回有效学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(semester);

        // 使用Moq.Protected模拟HttpClient请求
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{
                    ""success"": true,
                    ""studentTableVm"": {
                        ""activities"": [
                            {
                                ""id"": 1,
                                ""name"": ""Test Course"",
                                ""room"": ""Room 101"",
                                ""teacherName"": ""Teacher 1"",
                                ""weekIndexes"": [1, 2, 3],
                                ""dayOfWeek"": 1,
                                ""startSection"": 1,
                                ""sectionCount"": 2
                            }
                        ]
                    }
                }")
            });

        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _courseService.GetCoursesAsync(studentId, cookie);

        // Assert
        Assert.NotNull(result);
        // 因为有2个studentId，每个返回1门课程，所以应该有2门课程
        Assert.Equal(2, result.Count);
        // 验证httpClient被调用了2次
        httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当studentId包含无效格式时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldHandleInvalidStudentIdFormat()
    {
        // Arrange
        var studentId = "123456,,789012";
        var cookie = "test-cookie";
        var semester = new SemesterItem { Value = "2024-2025-1", Text = "2024-2025学年第一学期" };

        // 设置ExamService返回有效学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(semester);

        // 使用Moq.Protected模拟HttpClient请求
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{
                    ""success"": true,
                    ""studentTableVm"": {
                        ""activities"": [
                            {
                                ""id"": 1,
                                ""name"": ""Test Course"",
                                ""room"": ""Room 101"",
                                ""teacherName"": ""Teacher 1"",
                                ""weekIndexes"": [1, 2, 3],
                                ""dayOfWeek"": 1,
                                ""startSection"": 1,
                                ""sectionCount"": 2
                            }
                        ]
                    }
                }")
            });

        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act
        var result = await _courseService.GetCoursesAsync(studentId, cookie);

        // Assert
        Assert.NotNull(result);
        // 验证至少返回了课程
        Assert.True(result.Count > 0);
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当API返回404错误时是否抛出HttpRequestException
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowHttpRequestException_WhenApiReturns404()
    {
        // Arrange
        var studentId = "123456";
        var cookie = "test-cookie";
        var semester = new SemesterItem { Value = "2024-2025-1", Text = "2024-2025学年第一学期" };

        // 设置ExamService返回有效学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(semester);

        // 使用Moq.Protected模拟HttpClient请求返回404
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当API返回无效JSON时是否抛出异常
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowException_WhenApiReturnsInvalidJson()
    {
        // Arrange
        var studentId = "123456";
        var cookie = "test-cookie";
        var semester = new SemesterItem { Value = "2024-2025-1", Text = "2024-2025学年第一学期" };

        // 设置ExamService返回有效学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(semester);

        // 使用Moq.Protected模拟HttpClient请求返回无效JSON
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }

    /// <summary>
    /// 测试GetCoursesAsync方法，验证当API返回空课程数据时是否抛出InvalidOperationException
    /// </summary>
    [Fact]
    public async Task GetCoursesAsync_ShouldThrowInvalidOperationException_WhenApiReturnsEmptyCourses()
    {
        // Arrange
        var studentId = "123456";
        var cookie = "test-cookie";
        var semester = new SemesterItem { Value = "2024-2025-1", Text = "2024-2025学年第一学期" };

        // 设置ExamService返回有效学期
        _examServiceMock.Setup(x => x.GetThisSemester(cookie))
            .ReturnsAsync(semester);

        // 使用Moq.Protected模拟HttpClient请求返回空课程数据
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{
                    ""success"": true,
                    ""studentTableVm"": {
                        ""activities"": []
                    }
                }")
            });

        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _courseService.GetCoursesAsync(studentId, cookie));
    }
}