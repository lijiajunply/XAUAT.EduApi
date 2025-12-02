namespace XAUAT.EduApi.ServiceDiscovery;

/// <summary>
/// 服务注册中心接口，定义服务注册、注销、更新和获取服务实例的基本操作
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// 注册服务实例
    /// </summary>
    /// <param name="instance">服务实例</param>
    /// <returns>注册是否成功</returns>
    Task<bool> RegisterAsync(ServiceInstance instance);
    
    /// <summary>
    /// 注销服务实例
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="instanceId">实例ID</param>
    /// <returns>注销是否成功</returns>
    Task<bool> DeregisterAsync(string serviceName, string instanceId);
    
    /// <summary>
    /// 更新服务实例
    /// </summary>
    /// <param name="instance">服务实例</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateAsync(ServiceInstance instance);
    
    /// <summary>
    /// 获取服务的所有实例
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务实例列表</returns>
    Task<IReadOnlyList<ServiceInstance>> GetInstancesAsync(string serviceName);
    
    /// <summary>
    /// 获取所有服务名称
    /// </summary>
    /// <returns>服务名称列表</returns>
    Task<IReadOnlyList<string>> GetServicesAsync();
    
    /// <summary>
    /// 发送心跳，更新服务实例的健康状态
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="instanceId">实例ID</param>
    /// <returns>心跳是否成功</returns>
    Task<bool> SendHeartbeatAsync(string serviceName, string instanceId);
    
    /// <summary>
    /// 更新服务实例的状态
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="instanceId">实例ID</param>
    /// <param name="status">新的状态</param>
    /// <returns>更新是否成功</returns>
    Task<bool> UpdateStatusAsync(string serviceName, string instanceId, ServiceInstanceStatus status);
}
