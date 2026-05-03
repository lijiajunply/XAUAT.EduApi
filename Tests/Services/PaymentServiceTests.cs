using EduApi.Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using XAUAT.EduApi.Services;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Exceptions;

namespace XAUAT.EduApi.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        // 默认返回空token，防止工厂执行
        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("default-token");

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentModel>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentModel>());

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<double>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.0);

        _paymentService = new PaymentService(
            _cacheServiceMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    private void SetupLoginPassThrough()
    {
        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .Returns(async (string key, Func<Task<string>> factory, TimeSpan? exp, CacheLevel level, int priority, CancellationToken ct, bool isUse) =>
                await factory());
    }

    private void SetupPaymentsPassThrough()
    {
        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<PaymentModel>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .Returns(async (string key, Func<Task<List<PaymentModel>>> factory, TimeSpan? exp, CacheLevel level, int priority, CancellationToken ct, bool isUse) =>
                await factory());
    }

    private void SetupBalancePassThrough()
    {
        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<double>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .Returns(async (string key, Func<Task<double>> factory, TimeSpan? exp, CacheLevel level, int priority, CancellationToken ct, bool isUse) =>
                await factory());
    }

    private HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(content) });
        return new HttpClient(handler.Object);
    }

    private HttpClient CreateMockHttpClientThrowing(Exception ex)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);
        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task Login_ShouldHandleEmptyCardNum()
    {
        var cardNum = string.Empty;
        // 空cardNum不在Login中校验，直接走GetOrCreateAsync返回默认值
        var result = await _paymentService.Login(cardNum);
        Assert.Equal("default-token", result);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleEmptyCardNum()
    {
        var cardNum = string.Empty;
        // 空cardNum走GetTurnoverAsync → Login(返回默认token) → GetPayments + GetBalance
        var result = await _paymentService.GetTurnoverAsync(cardNum);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Login_ShouldReturnTokenFromCache_WhenTokenExists()
    {
        var cardNum = "123456";
        var expectedToken = "test-token-from-cache";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        var result = await _paymentService.Login(cardNum);

        Assert.Equal(expectedToken, result);
    }

    [Fact]
    public async Task Login_ShouldHandleRedisFailure()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.NotFound, ""));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }

    [Fact]
    public async Task Login_ShouldHandleHttpRequestTimeout()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClientThrowing(
                new TaskCanceledException("The operation was canceled.", new TimeoutException("The operation timed out after 1000ms"))));

        var exception = await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
        Assert.Contains("登录失败", exception.Message);
    }

    [Fact]
    public async Task Login_ShouldHandleHttpErrorStatus()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.InternalServerError, ""));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleLoginFailure()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.NotFound, ""));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }

    [Fact]
    public async Task Login_ShouldHandleNullCardNum()
    {
        var cardNum = (string)null;
        var result = await _paymentService.Login(cardNum);
        Assert.Equal("default-token", result);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleNullCardNum()
    {
        var cardNum = (string)null;
        var result = await _paymentService.GetTurnoverAsync(cardNum);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleInvalidApiResponseFormat()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        SetupPaymentsPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, "<html>Invalid JSON Response</html>"));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }

    [Fact]
    public async Task Login_ShouldHandleLargeCardNum()
    {
        var cardNum = "12345678901234567890";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""access_token"": ""test-token""}"));

        var result = await _paymentService.Login(cardNum);
        Assert.Equal("test-token", result);
    }

    [Fact]
    public async Task Login_ShouldHandleSmallCardNum()
    {
        var cardNum = "1";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""access_token"": ""test-token""}"));

        var result = await _paymentService.Login(cardNum);
        Assert.Equal("test-token", result);
    }

    [Fact]
    public async Task Login_ShouldHandleInvalidTokenFromCache()
    {
        var cardNum = "123456";
        var invalidToken = "invalid-token-format";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidToken);

        var result = await _paymentService.Login(cardNum);
        Assert.Equal(invalidToken, result);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleZeroBalance()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<double>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.0);

        SetupPaymentsPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""data"": {""records"": []}}"));

        var result = await _paymentService.GetTurnoverAsync(cardNum);

        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Empty(result.Records);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleNegativeBalance()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<double>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1.0);

        SetupPaymentsPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""data"": {""records"": []}}"));

        var result = await _paymentService.GetTurnoverAsync(cardNum);

        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Empty(result.Records);
        Assert.Equal(-1, result.Total);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleLargePaymentList()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<double>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10.0);

        SetupPaymentsPassThrough();

        var recordsJson = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            recordsJson.Add(@"{""turnoverType"":""消费"",""jndatetimeStr"":""2023-01-01 12:00:00"",""resume"":""测试消费"",""tranamt"":1000}");
        }

        var paymentListJson = @"{""data"":{""records"":[" + string.Join(",", recordsJson) + @"]}}";

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, paymentListJson));

        var result = await _paymentService.GetTurnoverAsync(cardNum);

        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Equal(50, result.Records.Count);
        Assert.Equal(10, result.Total);
    }

    [Fact]
    public async Task Login_ShouldHandleRedisSetFailure()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""access_token"": ""test-token""}"));

        var result = await _paymentService.Login(cardNum);
        Assert.Equal("test-token", result);
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleGetPaymentsException()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        SetupPaymentsPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClientThrowing(new HttpRequestException("Network error")));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleGetBalanceException()
    {
        var cardNum = "123456";
        var token = "test-token";

        _cacheServiceMock
            .Setup(m => m.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<string>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        SetupPaymentsPassThrough();
        SetupBalancePassThrough();

        var requestCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => {
                requestCount++;
                if (requestCount == 1) {
                    return new HttpResponseMessage {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(@"{""data"": {""records"": [{""id"": 1, ""transTime"": ""2023-01-01 12:00:00"", ""transAmount"": 10.0, ""transType"": ""消费"", ""transDesc"": ""测试消费"", ""balance"": 100.0}]}}")
                    };
                } else {
                    throw new HttpRequestException("Network error");
                }
            });

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }

    [Fact]
    public async Task Login_ShouldHandleKeyboardRequestFailure()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.NotFound, ""));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }

    [Fact]
    public async Task Login_ShouldHandleInvalidKeyboardData()
    {
        var cardNum = "123456";
        SetupLoginPassThrough();

        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>()))
            .Returns(CreateMockHttpClient(HttpStatusCode.OK, @"{""data"": {""invalidField"": ""value""}}"));

        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }

    [Fact]
    public async Task GetTurnoverAsync_ShouldReturnTestFixtureData_WhenTestAccountMatched()
    {
        var resolver = new Mock<ITestAccountResolver>();
        resolver.Setup(x => x.IsTestAccount(null, null, "20239999")).Returns(true);

        var provider = new Mock<ITestDataProvider>();
        provider.Setup(x => x.GetPaymentTurnoverAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PaymentModel("消费", "2026-05-01 12:30:00", "测试食堂午餐", 18.5)
            ]);
        provider.Setup(x => x.GetPaymentBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(128.5);

        var service = new PaymentService(
            _cacheServiceMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            resolver.Object,
            provider.Object);

        var result = await service.GetTurnoverAsync("20239999");

        Assert.Single(result.Records);
        Assert.Equal(128.5, result.Total);
        _cacheServiceMock.Verify(x => x.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<string>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CacheLevel>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }
}
