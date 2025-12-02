namespace XAUAT.EduApi.Extensions;

/// <summary>
/// 服务配置类
/// 用于封装所有服务配置信息
/// </summary>
public class ServiceConfiguration
{
    /// <summary>
    /// SQL连接字符串
    /// </summary>
    public string? SqlConnectionString { get; set; }
    
    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string? RedisConnectionString { get; set; }
    
    /// <summary>
    /// 是否启用Prometheus监控
    /// </summary>
    public bool EnablePrometheus { get; set; } = true;
    
    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// 是否启用健康检查
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
    
    /// <summary>
    /// 从配置中创建ServiceConfiguration实例
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>ServiceConfiguration实例</returns>
    public static ServiceConfiguration FromConfiguration(IConfiguration configuration)
    {
        return new ServiceConfiguration
        {
            SqlConnectionString = Environment.GetEnvironmentVariable("SQL", EnvironmentVariableTarget.Process),
            RedisConnectionString = Environment.GetEnvironmentVariable("REDIS", EnvironmentVariableTarget.Process) ?? 
                                   (configuration["Redis"] != null ? configuration["Redis"] : null),
            EnablePrometheus = configuration.GetValue<bool>("Prometheus:Enabled", true),
            EnableLogging = configuration.GetValue<bool>("Logging:Enabled", true),
            EnableHealthChecks = configuration.GetValue<bool>("HealthChecks:Enabled", true)
        };
    }
}