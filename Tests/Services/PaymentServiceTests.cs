using StackExchange.Redis;
using Moq;
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
    private readonly PaymentService _paymentService;

    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public PaymentServiceTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // 设置Redis连接返回模拟的数据库
        _connectionMultiplexerMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _paymentService = new PaymentService(
            _connectionMultiplexerMock.Object,
            _httpClientFactoryMock.Object);
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
}
