using EduApi.Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// CourseService单元测试
/// </summary>
public class CourseServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<CourseService>> _loggerMock;
    private readonly Mock<IExamService> _examServiceMock;
    private readonly CourseService _courseService;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public CourseServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CourseService>>();
        _examServiceMock = new Mock<IExamService>();

        // 设置HttpClientFactory返回一个有效的HttpClient实例
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        _courseService = new CourseService(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _examServiceMock.Object);
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
}
