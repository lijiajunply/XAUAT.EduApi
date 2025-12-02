using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace XAUAT.EduApi.HealthChecks;

/// <summary>
/// Redis健康检查
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<RedisHealthCheck> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redisConnection">Redis连接</param>
    /// <param name="logger">日志记录器</param>
    public RedisHealthCheck(IConnectionMultiplexer? redisConnection, ILogger<RedisHealthCheck> logger)
    {
        _redisConnection = redisConnection;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行健康检查
    /// </summary>
    /// <param name="context">健康检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // 如果Redis未配置，返回健康状态
        if (_redisConnection == null)
        {
            _logger.LogInformation("Redis未配置，跳过健康检查");
            return HealthCheckResult.Healthy("Redis未配置");
        }
        
        try
        {
            // 检查Redis连接
            var isConnected = _redisConnection.IsConnected;
            if (!isConnected)
            {
                _logger.LogWarning("Redis连接失败");
                return HealthCheckResult.Unhealthy("Redis连接失败");
            }
            
            // 执行Redis命令，检查Redis服务是否正常
            var db = _redisConnection.GetDatabase();
            var result = await db.PingAsync();
            
            if (result.TotalMilliseconds > 1000)
            {
                _logger.LogWarning("Redis响应时间过长: {ResponseTime}ms", result.TotalMilliseconds);
                return HealthCheckResult.Degraded($"Redis响应时间过长: {result.TotalMilliseconds}ms");
            }
            
            _logger.LogInformation("Redis健康检查通过，响应时间: {ResponseTime}ms", result.TotalMilliseconds);
            return HealthCheckResult.Healthy($"Redis连接正常，响应时间: {result.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis健康检查失败");
            return HealthCheckResult.Unhealthy("Redis健康检查失败", ex);
        }
    }
}
