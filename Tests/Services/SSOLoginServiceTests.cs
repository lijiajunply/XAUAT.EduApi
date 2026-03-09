using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Interfaces;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class SSOLoginServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ICookieCodeService> _mockCookieCodeService;
    private readonly Mock<ILogger<SSOLoginService>> _mockLogger;
    private readonly SSOLoginService _service;

    public SSOLoginServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockCookieCodeService = new Mock<ICookieCodeService>();
        _mockLogger = new Mock<ILogger<SSOLoginService>>();
        _service = new SSOLoginService(_mockHttpClientFactory.Object, _mockCookieCodeService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnResult_WhenLoginIsSuccessful()
    {
        // Arrange
        var username = "user";
        var password = "password";
        var cookies = "cookie_string";
        var studentId = "123456";

        var responseContent = JsonConvert.SerializeObject(new
        {
            success = true,
            cookies = cookies
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(responseContent)
           });

        var client = new HttpClient(handlerMock.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        _mockCookieCodeService.Setup(x => x.GetCode(cookies)).ReturnsAsync(studentId);

        // Act
        var result = await _service.LoginAsync(username, password);

        // Assert
        Assert.NotNull(result);
        var propertyInfo = result.GetType().GetProperty("Success");
        Assert.True((bool)propertyInfo!.GetValue(result)!);
        
        var studentIdProp = result.GetType().GetProperty("StudentId");
        Assert.Equal(studentId, studentIdProp!.GetValue(result));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenApiReturnsFailure()
    {
        // Arrange
        var username = "user";
        var password = "password";

        var responseContent = JsonConvert.SerializeObject(new
        {
            success = false
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(responseContent)
           });

        var client = new HttpClient(handlerMock.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        // Act & Assert
        await Assert.ThrowsAsync<LoginFailedException>(() => _service.LoginAsync(username, password));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenHttpFailure()
    {
        // Arrange
        var username = "user";
        var password = "password";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.InternalServerError
           });

        var client = new HttpClient(handlerMock.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.LoginAsync(username, password));
    }
}
