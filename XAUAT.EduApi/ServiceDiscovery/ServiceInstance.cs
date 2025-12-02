namespace XAUAT.EduApi.ServiceDiscovery;

/// <summary>
/// 服务实例状态
/// </summary>
public enum ServiceInstanceStatus
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 健康
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// 正在启动
    /// </summary>
    Starting = 3,

    /// <summary>
    /// 正在停止
    /// </summary>
    Stopping = 4,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 5
}

/// <summary>
/// 服务实例，用于表示一个服务实例的信息
/// </summary>
[Serializable]
public class ServiceInstance
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// 实例ID
    /// </summary>
    public required string InstanceId { get; set; }

    /// <summary>
    /// 实例地址
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// 实例端口
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// 实例状态
    /// </summary>
    public ServiceInstanceStatus Status { get; set; } = ServiceInstanceStatus.Healthy;

    /// <summary>
    /// 实例是否启用了HTTPS
    /// </summary>
    public bool IsHttps { get; set; }

    /// <summary>
    /// 实例的元数据
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// 实例的最后注册时间
    /// </summary>
    public DateTime LastRegistered { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 实例的最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 实例的健康检查地址
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// 获取实例的完整URL
    /// </summary>
    /// <returns>实例的完整URL</returns>
    public string GetUrl()
    {
        var scheme = IsHttps ? "https" : "http";
        return $"{scheme}://{Host}:{Port}";
    }
}