using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Moq;
using Moq.Protected;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// PaymentService单元测试
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public PaymentServiceTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        // 设置Redis连接返回模拟的数据库
        _connectionMultiplexerMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _paymentService = new PaymentService(
            _connectionMultiplexerMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// 测试Login方法，验证当cardNum为空时是否处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleEmptyCardNum()
    {
        // Arrange
        var cardNum = string.Empty;

        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => 
            _paymentService.Login(cardNum));
    }

    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当cardNum为空时是否处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleEmptyCardNum()
    {
        // Arrange
        var cardNum = string.Empty;

        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => 
            _paymentService.GetTurnoverAsync(cardNum));
    }

    /// <summary>
    /// 测试Login方法，验证当Redis中存在token时是否直接返回
    /// </summary>
    [Fact]
    public async Task Login_ShouldReturnTokenFromRedis_WhenTokenExists()
    {
        // Arrange
        var cardNum = "123456";
        var expectedToken = "test-token-from-redis";

        // 设置Redis返回存在的token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _paymentService.Login(cardNum);

        // Assert
        Assert.Equal(expectedToken, result);
        // 验证没有调用HttpClient
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }
    
    /// <summary>
    /// 测试Login方法，验证当Redis操作失败时是否能正常处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleRedisFailure()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis操作失败
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new Exception("Redis operation timed out"));
        
        // 模拟HttpClient返回404
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.NotFound });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }
    
    /// <summary>
    /// 测试Login方法，验证当HTTP请求超时时有正确的异常处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleHttpRequestTimeout()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient请求超时
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The operation was canceled.", new TimeoutException("The operation timed out after 1000ms")));
        var httpClient = new HttpClient(httpClientHandler.Object);
        httpClient.Timeout = TimeSpan.FromMilliseconds(1000);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
        Assert.Contains("请求超时", exception.Message);
    }
    
    /// <summary>
    /// 测试Login方法，验证当HTTP请求返回错误状态码时有正确的异常处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleHttpErrorStatus()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient返回500错误
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当Login失败时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleLoginFailure()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient返回404
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.NotFound });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }
    
    
    
    /// <summary>
    /// 测试Login方法，验证当cardNum为null时有正确的异常处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleNullCardNum()
    {
        // Arrange
        var cardNum = (string)null;

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当cardNum为null时有正确的异常处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleNullCardNum()
    {
        // Arrange
        var cardNum = (string)null;

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当API返回格式错误时有正确的异常处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleInvalidApiResponseFormat()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        
        // 模拟HttpClient返回无效JSON
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("<html>Invalid JSON Response</html>")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }
    
    /// <summary>
    /// 测试Login方法，验证当cardNum为极大值时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleLargeCardNum()
    {
        // Arrange
        var cardNum = "12345678901234567890";
        var token = "test-token";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient返回token
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": ""test-token""}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.Login(cardNum);

        // Assert
        Assert.Equal("test-token", result);
    }
    
    /// <summary>
    /// 测试Login方法，验证当cardNum为极小值时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleSmallCardNum()
    {
        // Arrange
        var cardNum = "1";
        var token = "test-token";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient返回token
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": ""test-token""}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.Login(cardNum);

        // Assert
        Assert.Equal("test-token", result);
    }
    
    /// <summary>
    /// 测试Login方法，验证当Redis返回无效token格式时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleInvalidTokenFromRedis()
    {
        // Arrange
        var cardNum = "123456";
        var invalidToken = "invalid-token-format";
        
        // 设置Redis返回无效token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(invalidToken);
        
        // 模拟HttpClient返回有效token
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": ""valid-token""}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.Login(cardNum);

        // Assert
        Assert.Equal(invalidToken, result);
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当GetBalanceAsync返回0时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleZeroBalance()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        // 设置Redis返回空支付列表
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"paymentList-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        // 设置Redis返回0余额
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"ele-acc-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync("0");
        
        // 模拟HttpClient返回空支付列表
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""data"": {""records"": []}}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.GetTurnoverAsync(cardNum);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Empty(result.Records);
        Assert.Equal(0, result.Total);
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当GetBalanceAsync返回负数时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleNegativeBalance()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        // 设置Redis返回空支付列表
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"paymentList-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        // 设置Redis返回负余额
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"ele-acc-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync("-100");
        
        // 模拟HttpClient返回空支付列表
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""data"": {""records"": []}}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.GetTurnoverAsync(cardNum);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Empty(result.Records);
        Assert.Equal(-1, result.Total); // 因为返回的是-100，除以100后是-1
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当GetPayments返回大量数据时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleLargePaymentList()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        // 设置Redis返回空支付列表
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"paymentList-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        // 设置Redis返回余额
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"ele-acc-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync("1000");
        
        // 构建大量支付记录
        var records = new List<object>();
        for (int i = 0; i < 50; i++)
        {
            records.Add(new {
                id = i,
                transTime = "2023-01-01 12:00:00",
                transAmount = 10.0,
                transType = "消费",
                transDesc = "测试消费",
                balance = 100.0
            });
        }
        
        var paymentListJson = System.Text.Json.JsonSerializer.Serialize(new {
            data = new {
                records = records
            }
        });
        
        // 模拟HttpClient返回大量支付记录
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(paymentListJson)
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.GetTurnoverAsync(cardNum);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
        Assert.Equal(50, result.Records.Count);
        Assert.Equal(10, result.Total); // 因为返回的是1000，除以100后是10
    }
    
    /// <summary>
    /// 测试Login方法，验证当Redis StringSetAsync失败时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleRedisSetFailure()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        // 设置Redis StringSetAsync失败
        _redisDatabaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ThrowsAsync(new Exception("Redis operation timed out"));
        
        // 模拟HttpClient返回token
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": ""test-token""}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _paymentService.Login(cardNum);

        // Assert
        Assert.Equal("test-token", result);
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当GetPayments抛出异常时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleGetPaymentsException()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        // 设置Redis返回空支付列表
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"paymentList-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient在GetPayments请求时抛出异常
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }
    
    /// <summary>
    /// 测试GetTurnoverAsync方法，验证当GetBalanceAsync抛出异常时是否能正确处理
    /// </summary>
    [Fact]
    public async Task GetTurnoverAsync_ShouldHandleGetBalanceException()
    {
        // Arrange
        var cardNum = "123456";
        var token = "test-token";
        
        // 设置Redis返回token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(token);
        // 设置Redis返回空支付列表
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"paymentList-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        // 设置Redis返回空余额
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"ele-acc-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient在第一次请求（GetPayments）时返回有效数据，在第二次请求（GetBalanceAsync）时抛出异常
        var requestCount = 0;
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => {
                requestCount++;
                if (requestCount == 1) {
                    // 第一次请求返回支付列表
                    return new HttpResponseMessage {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(@"{""data"": {""records"": [{""id"": 1, ""transTime"": ""2023-01-01 12:00:00"", ""transAmount"": 10.0, ""transType"": ""消费"", ""transDesc"": ""测试消费"", ""balance"": 100.0}]}}")
                    };
                } else {
                    // 第二次请求抛出异常
                    throw new HttpRequestException("Network error");
                }
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.GetTurnoverAsync(cardNum));
    }
    
    /// <summary>
    /// 测试Login方法，验证当键盘数据请求失败时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleKeyboardRequestFailure()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient在键盘数据请求时返回404
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.NotFound
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }
    
    /// <summary>
    /// 测试Login方法，验证当键盘数据格式错误时是否能正确处理
    /// </summary>
    [Fact]
    public async Task Login_ShouldHandleInvalidKeyboardData()
    {
        // Arrange
        var cardNum = "123456";
        
        // 设置Redis返回空值
        _redisDatabaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟HttpClient返回无效键盘数据
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{""data"": {""invalidField"": ""value""}}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentServiceException>(() => _paymentService.Login(cardNum));
    }
}