using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using XAUAT.EduApi.Services;
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
    private readonly Mock<IConnectionMultiplexer> _redisConnectionMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public PaymentServiceIntegrationTests()
    {
        // 使用内存数据库进行集成测试
        _dbContextOptions = new DbContextOptionsBuilder<EduContext>()
            .UseInMemoryDatabase(databaseName: "PaymentTestDatabase")
            .Options;

        // 创建数据库上下文
        _dbContext = new EduContext(_dbContextOptions);

        // 初始化数据库
        _dbContext.Database.EnsureCreated();

        // 创建Redis模拟
        _redisConnectionMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();

        // 设置Redis连接返回模拟的数据库
        _redisConnectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        // 创建HttpClientFactory模拟
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // 创建Logger模拟
        _loggerMock = new Mock<ILogger<PaymentService>>();

        // 模拟支付服务
        _paymentService = new PaymentService(
            _redisConnectionMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// 测试支付服务的完整流程：登录→获取消费记录
    /// </summary>
    [Fact]
    public async Task PaymentCompleteFlow_ShouldWorkCorrectly()
    {
        // Arrange
        var cardNum = "123456";
        var expectedToken = "test-token-123";
        
        // 设置Redis返回模拟的token
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedToken);
        
        // 模拟HttpClient
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"code\": 200, \"data\": {\"records\": [{\"id\": \"1\", \"transTime\": \"2024-01-01 12:00:00\", \"transAmount\": 10.5, \"transType\": \"消费\", \"remark\": \"食堂消费\"}, {\"id\": \"2\", \"transTime\": \"2024-01-02 13:00:00\", \"transAmount\": 15.0, \"transType\": \"消费\", \"remark\": \"超市消费\"}]}}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        // 设置第二次HttpClient调用返回余额数据
        httpClientHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"code\": 200, \"data\": {\"card\": [{\"elec_accamt\": 50000}]}}")
            })
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"code\": 200, \"data\": {\"card\": [{\"elec_accamt\": 50000}]}}")
            });

        // Act
        var token = await _paymentService.Login(cardNum);
        var paymentData = await _paymentService.GetTurnoverAsync(cardNum);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(expectedToken, token);
    }

    /// <summary>
    /// 测试支付服务登录流程，验证Redis缓存功能
    /// </summary>
    [Fact]
    public async Task Login_ShouldCacheTokenInRedis()
    {
        // Arrange
        var cardNum = "123456";
        var expectedToken = "test-token-456";
        
        // 设置Redis返回空值，表示token不存在
        _redisDatabaseMock.Setup(x => x.StringGetAsync($"payment-{cardNum}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // 模拟登录API返回有效的token
        var httpClientHandler = new Mock<HttpMessageHandler>();
        httpClientHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"access_token\": \"test-token-456\", \"token_type\": \"bearer\", \"expires_in\": 3600}")
            });
        var httpClient = new HttpClient(httpClientHandler.Object);
        _httpClientFactoryMock.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        // 模拟Redis写入操作
        _redisDatabaseMock.Setup(x => x.StringSetAsync(
            $"payment-{cardNum}", 
            expectedToken, 
            It.IsAny<TimeSpan>(), 
            It.IsAny<When>(), 
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var token = await _paymentService.Login(cardNum);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(expectedToken, token);
        
        // 验证Redis写入操作被调用
        _redisDatabaseMock.Verify(x => x.StringSetAsync(
            $"payment-{cardNum}", 
            expectedToken, 
            It.IsAny<TimeSpan>(), 
            It.IsAny<When>(), 
            It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    /// 清理测试资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
