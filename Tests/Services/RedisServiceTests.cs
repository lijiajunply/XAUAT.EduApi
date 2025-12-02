using Moq;
using StackExchange.Redis;
using XAUAT.EduApi.Services;
using Xunit;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// RedisService单元测试
/// </summary>
public class RedisServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly RedisService _redisService;
    
    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public RedisServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        
        // 模拟Redis数据库
        var redisDatabaseMock = new Mock<IDatabase>();
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 创建RedisService实例
        _redisService = new RedisService(_redisMock.Object);
    }
    
    /// <summary>
    /// 测试GetKeyValueAsync方法，验证当Redis可用时是否能正确获取值
    /// </summary>
    [Fact]
    public async Task GetKeyValueAsync_ShouldReturnValue_WhenRedisIsAvailable()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        
        // 模拟Redis数据库
        var redisDatabaseMock = new Mock<IDatabase>();
        redisDatabaseMock.Setup(m => m.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedValue);
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 创建RedisService实例
        var redisService = new RedisService(_redisMock.Object);
        
        // Act
        var result = await redisService.GetKeyValueAsync(key);
        
        // Assert
        Assert.Equal(expectedValue, result);
    }
    
    /// <summary>
    /// 测试GetKeyValueAsync方法，验证当Redis不可用时是否返回空字符串
    /// </summary>
    [Fact]
    public async Task GetKeyValueAsync_ShouldReturnEmptyString_WhenRedisIsNotAvailable()
    {
        // Arrange
        var key = "test-key";
        
        // 模拟Redis数据库返回null
        var redisDatabaseMock = new Mock<IDatabase>();
        redisDatabaseMock.Setup(m => m.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        _redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDatabaseMock.Object);
        
        // 创建RedisService实例
        var redisService = new RedisService(_redisMock.Object);
        
        // Act
        var result = await redisService.GetKeyValueAsync(key);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }
    
    /// <summary>
    /// 测试RedisService构造函数，验证当muxer为null时是否能正常创建实例
    /// </summary>
    [Fact]
    public void RedisService_ShouldBeCreated_WhenMuxerIsNull()
    {
        // Arrange
        IConnectionMultiplexer? muxer = null;
        
        // Act
        var redisService = new RedisService(muxer);
        
        // Assert
        Assert.NotNull(redisService);
    }
}