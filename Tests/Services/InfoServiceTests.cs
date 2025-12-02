using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

/// <summary>
/// InfoService单元测试
/// </summary>
public class InfoServiceTests
{
    private readonly InfoService _infoService;
    
    /// <summary>
    /// 构造函数，初始化测试依赖
    /// </summary>
    public InfoServiceTests()
    {
        _infoService = new InfoService();
    }
    
    /// <summary>
    /// 测试GetTime方法，验证是否能正确获取时间范围
    /// </summary>
    [Fact]
    public void GetTime_ShouldReturnTimeModel_WhenCalled()
    {
        // Act
        var result = _infoService.GetTime();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.StartTime);
        Assert.NotNull(result.EndTime);
        Assert.NotEmpty(result.StartTime);
        Assert.NotEmpty(result.EndTime);
    }
    
    /// <summary>
    /// 测试GetTime方法，验证当环境变量存在时是否能正确获取环境变量中的值
    /// </summary>
    [Fact]
    public void GetTime_ShouldReturnEnvironmentValues_WhenEnvironmentVariablesExist()
    {
        // Arrange
        const string expectedStartTime = "2025-01-01";
        const string expectedEndTime = "2025-12-31";
        
        // 设置环境变量
        Environment.SetEnvironmentVariable("START", expectedStartTime, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", expectedEndTime, EnvironmentVariableTarget.Process);
        
        // Act
        var result = _infoService.GetTime();
        
        // Assert
        Assert.Equal(expectedStartTime, result.StartTime);
        Assert.Equal(expectedEndTime, result.EndTime);
        
        // 清理环境变量
        Environment.SetEnvironmentVariable("START", null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", null, EnvironmentVariableTarget.Process);
    }
    
    /// <summary>
    /// 测试IsInSchool方法，验证当当前时间在时间范围内时是否返回true
    /// </summary>
    [Fact]
    public void IsInSchool_ShouldReturnTrue_WhenCurrentTimeIsInRange()
    {
        // Arrange
        const string startTime = "2000-01-01";
        const string endTime = "2100-12-31";
        
        // 设置环境变量
        Environment.SetEnvironmentVariable("START", startTime, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", endTime, EnvironmentVariableTarget.Process);
        
        // Act
        var result = _infoService.IsInSchool();
        
        // Assert
        Assert.True(result);
        
        // 清理环境变量
        Environment.SetEnvironmentVariable("START", null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", null, EnvironmentVariableTarget.Process);
    }
    
    /// <summary>
    /// 测试IsInSchool方法，验证当当前时间不在时间范围内时是否返回false
    /// </summary>
    [Fact]
    public void IsInSchool_ShouldReturnFalse_WhenCurrentTimeIsOutOfRange()
    {
        // Arrange
        const string startTime = "1900-01-01";
        const string endTime = "1999-12-31";
        
        // 设置环境变量
        Environment.SetEnvironmentVariable("START", startTime, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", endTime, EnvironmentVariableTarget.Process);
        
        // Act
        var result = _infoService.IsInSchool();
        
        // Assert
        Assert.False(result);
        
        // 清理环境变量
        Environment.SetEnvironmentVariable("START", null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("END", null, EnvironmentVariableTarget.Process);
    }
}