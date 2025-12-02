namespace XAUAT.EduApi.ServiceDiscovery;

/// <summary>
/// 基于内存的服务注册中心实现
/// </summary>
public class InMemoryServiceRegistry : IServiceRegistry
{
    private readonly ILogger<InMemoryServiceRegistry> _logger;
    private readonly Dictionary<string, List<ServiceInstance>> _services = [];
    private readonly Dictionary<string, DateTime> _heartbeats = [];
    private readonly object _lock = new();
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);
    private Timer? _cleanupTimer;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public InMemoryServiceRegistry(ILogger<InMemoryServiceRegistry> logger)
    {
        _logger = logger;
        StartCleanupTimer();
    }
    
    /// <summary>
    /// 启动清理定时器，定期清理过期的服务实例
    /// </summary>
    private void StartCleanupTimer()
    {
        _cleanupTimer = new Timer(
            CleanupExpiredInstances,
            null,
            _cleanupInterval,
            _cleanupInterval);
        
        _logger.LogInformation("Cleanup timer started with interval: {Interval}", _cleanupInterval);
    }
    
    /// <summary>
    /// 清理过期的服务实例
    /// </summary>
    /// <param name="state">定时器状态</param>
    private void CleanupExpiredInstances(object? state)
    {
        _logger.LogInformation("Cleaning up expired service instances");
        
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var expiredServices = new List<string>();
            
            foreach (var (serviceName, instances) in _services)
            {
                var expiredInstances = instances.Where(instance => 
                {
                    var heartbeatKey = $"{serviceName}:{instance.InstanceId}";
                    return _heartbeats.TryGetValue(heartbeatKey, out var lastHeartbeat) && 
                           now - lastHeartbeat > _heartbeatTimeout;
                }).ToList();
                
                foreach (var expiredInstance in expiredInstances)
                {
                    instances.Remove(expiredInstance);
                    var heartbeatKey = $"{serviceName}:{expiredInstance.InstanceId}";
                    _heartbeats.Remove(heartbeatKey);
                    
                    _logger.LogInformation("Cleaned up expired service instance: {ServiceName}::{InstanceId}", 
                        serviceName, expiredInstance.InstanceId);
                }
                
                if (instances.Count == 0)
                {
                    expiredServices.Add(serviceName);
                }
            }
            
            // 移除没有实例的服务
            foreach (var serviceName in expiredServices)
            {
                _services.Remove(serviceName);
                _logger.LogInformation("Removed empty service: {ServiceName}", serviceName);
            }
        }
    }
    
    /// <inheritdoc />
    public Task<bool> RegisterAsync(ServiceInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        
        _logger.LogInformation("Registering service instance: {ServiceName}::{InstanceId} at {Url}", 
            instance.ServiceName, instance.InstanceId, instance.GetUrl());
        
        lock (_lock)
        {
            if (!_services.TryGetValue(instance.ServiceName, out var instances))
            {
                instances = [];
                _services[instance.ServiceName] = instances;
            }
            
            // 检查实例是否已经存在
            var existingInstance = instances.FirstOrDefault(i => i.InstanceId == instance.InstanceId);
            if (existingInstance != null)
            {
                _logger.LogWarning("Service instance already registered: {ServiceName}::{InstanceId}", 
                    instance.ServiceName, instance.InstanceId);
                return Task.FromResult(false);
            }
            
            // 添加实例
            instances.Add(instance);
            
            // 更新心跳
            var heartbeatKey = $"{instance.ServiceName}:{instance.InstanceId}";
            _heartbeats[heartbeatKey] = DateTime.UtcNow;
            
            _logger.LogInformation("Service instance registered successfully: {ServiceName}::{InstanceId}", 
                instance.ServiceName, instance.InstanceId);
            
            return Task.FromResult(true);
        }
    }
    
    /// <inheritdoc />
    public Task<bool> DeregisterAsync(string serviceName, string instanceId)
    {
        _logger.LogInformation("Deregistering service instance: {ServiceName}::{InstanceId}", serviceName, instanceId);
        
        lock (_lock)
        {
            if (!_services.TryGetValue(serviceName, out var instances))
            {
                _logger.LogWarning("Service not found: {ServiceName}", serviceName);
                return Task.FromResult(false);
            }
            
            var instance = instances.FirstOrDefault(i => i.InstanceId == instanceId);
            if (instance == null)
            {
                _logger.LogWarning("Service instance not found: {ServiceName}::{InstanceId}", serviceName, instanceId);
                return Task.FromResult(false);
            }
            
            // 移除实例
            instances.Remove(instance);
            
            // 移除心跳
            var heartbeatKey = $"{serviceName}:{instanceId}";
            _heartbeats.Remove(heartbeatKey);
            
            // 如果服务没有实例了，移除服务
            if (instances.Count == 0)
            {
                _services.Remove(serviceName);
                _logger.LogInformation("Removed empty service: {ServiceName}", serviceName);
            }
            
            _logger.LogInformation("Service instance deregistered successfully: {ServiceName}::{InstanceId}", 
                serviceName, instanceId);
            
            return Task.FromResult(true);
        }
    }
    
    /// <inheritdoc />
    public Task<bool> UpdateAsync(ServiceInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        
        _logger.LogInformation("Updating service instance: {ServiceName}::{InstanceId}", 
            instance.ServiceName, instance.InstanceId);
        
        lock (_lock)
        {
            if (!_services.TryGetValue(instance.ServiceName, out var instances))
            {
                _logger.LogWarning("Service not found: {ServiceName}", instance.ServiceName);
                return Task.FromResult(false);
            }
            
            var existingInstance = instances.FirstOrDefault(i => i.InstanceId == instance.InstanceId);
            if (existingInstance == null)
            {
                _logger.LogWarning("Service instance not found: {ServiceName}::{InstanceId}", 
                    instance.ServiceName, instance.InstanceId);
                return Task.FromResult(false);
            }
            
            // 更新实例
            instances.Remove(existingInstance);
            instance.LastUpdated = DateTime.UtcNow;
            instances.Add(instance);
            
            // 更新心跳
            var heartbeatKey = $"{instance.ServiceName}:{instance.InstanceId}";
            _heartbeats[heartbeatKey] = DateTime.UtcNow;
            
            _logger.LogInformation("Service instance updated successfully: {ServiceName}::{InstanceId}", 
                instance.ServiceName, instance.InstanceId);
            
            return Task.FromResult(true);
        }
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyList<ServiceInstance>> GetInstancesAsync(string serviceName)
    {
        _logger.LogInformation("Getting service instances for: {ServiceName}", serviceName);
        
        lock (_lock)
        {
            if (_services.TryGetValue(serviceName, out var instances))
            {
                return Task.FromResult<IReadOnlyList<ServiceInstance>>(instances.AsReadOnly());
            }
            
            return Task.FromResult<IReadOnlyList<ServiceInstance>>(Array.Empty<ServiceInstance>());
        }
    }
    
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetServicesAsync()
    {
        _logger.LogInformation("Getting all service names");
        
        lock (_lock)
        {
            var serviceNames = _services.Keys.ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<string>>(serviceNames);
        }
    }
    
    /// <inheritdoc />
    public Task<bool> SendHeartbeatAsync(string serviceName, string instanceId)
    {
        _logger.LogDebug("Received heartbeat for: {ServiceName}::{InstanceId}", serviceName, instanceId);
        
        lock (_lock)
        {
            // 检查服务和实例是否存在
            if (_services.TryGetValue(serviceName, out var instances))
            {
                var instance = instances.FirstOrDefault(i => i.InstanceId == instanceId);
                if (instance != null)
                {
                    // 更新心跳
                    var heartbeatKey = $"{serviceName}:{instanceId}";
                    _heartbeats[heartbeatKey] = DateTime.UtcNow;
                    
                    // 更新实例状态为健康
                    instance.Status = ServiceInstanceStatus.Healthy;
                    
                    return Task.FromResult(true);
                }
            }
            
            _logger.LogWarning("Received heartbeat for non-existent service instance: {ServiceName}::{InstanceId}", 
                serviceName, instanceId);
            
            return Task.FromResult(false);
        }
    }
    
    /// <inheritdoc />
    public Task<bool> UpdateStatusAsync(string serviceName, string instanceId, ServiceInstanceStatus status)
    {
        _logger.LogInformation("Updating status for service instance: {ServiceName}::{InstanceId} to {Status}", 
            serviceName, instanceId, status);
        
        lock (_lock)
        {
            if (_services.TryGetValue(serviceName, out var instances))
            {
                var instance = instances.FirstOrDefault(i => i.InstanceId == instanceId);
                if (instance != null)
                {
                    // 更新实例状态
                    instance.Status = status;
                    instance.LastUpdated = DateTime.UtcNow;
                    
                    return Task.FromResult(true);
                }
            }
            
            _logger.LogWarning("Failed to update status for non-existent service instance: {ServiceName}::{InstanceId}", 
                serviceName, instanceId);
            
            return Task.FromResult(false);
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}