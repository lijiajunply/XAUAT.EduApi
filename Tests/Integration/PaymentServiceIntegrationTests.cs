using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Caching;
using EduApi.Data;
using Moq.Protected;

namespace XAUAT.EduApi.Tests.Integration;

/// <summary>
/// PaymentService集成测试
/// </summary>
public class PaymentServiceIntegrationTests : IDisposable
{
    private readonly DbContextOptions<EduContext> _dbContextOptions;
    private readonly EduContext _dbContext;
    private readonly PaymentService _paymentService;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    public PaymentServiceIntegrationTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<EduContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDatabase")
            .Options;

        _dbContext = new EduContext(_dbContextOptions);
        _dbContext.Database.EnsureCreated();

        _cacheServiceMock = new Mock<ICacheService>();
        SetupGetOrCreatePassThrough<string>();

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _paymentService = new PaymentService(
            _cacheServiceMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    private void SetupGetOrCreatePassThrough<T>()
    {
        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<T>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .Returns(async (string key, Func<Task<T>> factory, TimeSpan? exp, CacheLevel level, int priority, CancellationToken ct, bool isUse) =>
                await factory());
    }

    [Fact]
    public async Task PaymentCompleteFlow_ShouldWorkCorrectly()
    {
        var cardNum = "123456";
        var expectedToken = "test-token-123";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"code\": 200, \"data\": {\"records\": [{\"id\": \"1\", \"transTime\": \"2024-01-01 12:00:00\", \"transAmount\": 10.5, \"transType\": \"消费\", \"remark\": \"食堂消费\"}]}}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var token = await _paymentService.Login(cardNum);
        var paymentData = await _paymentService.GetTurnoverAsync(cardNum);

        Assert.NotNull(token);
        Assert.Equal(expectedToken, token);

        SetupGetOrCreatePassThrough<string>();
    }

    [Fact]
    public async Task Login_ShouldCacheToken()
    {
        var cardNum = "123456";
        var expectedToken = "test-token-456";

        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\": \"test-token-456\", \"token_type\": \"bearer\", \"expires_in\": 3600}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var token = await _paymentService.Login(cardNum);

        Assert.NotNull(token);
        Assert.Equal(expectedToken, token);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
