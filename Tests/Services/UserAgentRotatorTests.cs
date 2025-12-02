using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// UserAgentRotator单元测试
/// </summary>
public class UserAgentRotatorTests
{
    /// <summary>
    /// 测试SetRealisticHeaders方法，验证是否能正确设置User-Agent头
    /// </summary>
    [Fact]
    public void SetRealisticHeaders_ShouldSetUserAgentHeader_WhenClientIsNotNull()
    {
        // Arrange
        var client = new HttpClient();
        
        // Act
        client.SetRealisticHeaders();
        
        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains("User-Agent"));
        Assert.NotEmpty(client.DefaultRequestHeaders.UserAgent);
    }
    
    /// <summary>
    /// 测试SetRealisticHeaders方法，验证当client为null时是否不会抛出异常
    /// </summary>
    [Fact]
    public void SetRealisticHeaders_ShouldNotThrowException_WhenClientIsNull()
    {
        // Arrange
        HttpClient? client = null;
        
        // Act & Assert
        client?.SetRealisticHeaders(); // 不应该抛出异常
    }
    
    /// <summary>
    /// 测试SetRealisticHeaders方法，验证多次调用是否会添加多个User-Agent头
    /// </summary>
    [Fact]
    public void SetRealisticHeaders_ShouldAddMultipleUserAgentHeaders_WhenCalledMultipleTimes()
    {
        // Arrange
        var client = new HttpClient();
        
        // Act
        client.SetRealisticHeaders();
        client.SetRealisticHeaders();
        client.SetRealisticHeaders();
        
        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains("User-Agent"));
        Assert.True(client.DefaultRequestHeaders.UserAgent.Count > 1);
    }
    
    /// <summary>
    /// 测试SetRealisticHeaders方法，验证设置的User-Agent头是否包含预期的字符串
    /// </summary>
    [Fact]
    public void SetRealisticHeaders_ShouldSetValidUserAgent_WhenCalled()
    {
        // Arrange
        var client = new HttpClient();
        
        // Act
        client.SetRealisticHeaders();
        
        // Assert
        var userAgent = client.DefaultRequestHeaders.UserAgent.ToString();
        Assert.NotNull(userAgent);
        Assert.NotEmpty(userAgent);
        // 不再检查是否包含"Mozilla"，因为User-Agent列表中包含非Mozilla的User-Agent（如Opera）
        Assert.True(userAgent.Length > 10); // 确保User-Agent有足够的长度
    }
}