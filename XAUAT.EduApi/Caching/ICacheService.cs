using System.Runtime.CompilerServices;

namespace XAUAT.EduApi.Caching;

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存项，如果不存在则返回null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取缓存项，如果不存在则通过工厂方法创建并缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="level">缓存级别</param>
    /// <param name="businessPriority">业务优先级（1-10，10最高）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存项</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, 
        CacheLevel level = CacheLevel.MultiLevel, int businessPriority = 5, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 设置缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="level">缓存级别</param>
    /// <param name="businessPriority">业务优先级（1-10，10最高）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetAsync<T>(string key, T? value, TimeSpan? expiration = null, 
        CacheLevel level = CacheLevel.MultiLevel, int businessPriority = 5, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    Task<bool> RemoveAsync(string key, CacheLevel level = CacheLevel.MultiLevel, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string key, CacheLevel level = CacheLevel.MultiLevel, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 清除所有缓存项
    /// </summary>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否清除成功</returns>
    Task<bool> ClearAsync(CacheLevel level = CacheLevel.MultiLevel, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="keys">缓存键列表</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存项字典</returns>
    Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, 
        CacheLevel level = CacheLevel.MultiLevel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量设置缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="items">缓存项字典</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="level">缓存级别</param>
    /// <param name="businessPriority">业务优先级（1-10，10最高）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetManyAsync<T>(IDictionary<string, T?> items, TimeSpan? expiration = null, 
        CacheLevel level = CacheLevel.MultiLevel, int businessPriority = 5, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量移除缓存项
    /// </summary>
    /// <param name="keys">缓存键列表</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    Task<bool> RemoveManyAsync(IEnumerable<string> keys, 
        CacheLevel level = CacheLevel.MultiLevel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存统计信息</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 刷新缓存项（更新最后访问时间和过期时间）
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="newExpiration">新的过期时间</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否刷新成功</returns>
    Task<bool> RefreshAsync(string key, TimeSpan? newExpiration = null, 
        CacheLevel level = CacheLevel.MultiLevel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 搜索缓存键
    /// </summary>
    /// <param name="pattern">键匹配模式</param>
    /// <param name="level">缓存级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的缓存键列表</returns>
    IAsyncEnumerable<string> SearchKeysAsync(string pattern, 
        CacheLevel level = CacheLevel.MultiLevel, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    
    #region 缓存预热相关方法
    
    /// <summary>
    /// 添加缓存预热任务
    /// </summary>
    /// <param name="warmupItem">预热任务项</param>
    void AddWarmupTask(CacheWarmupItem warmupItem);
    
    /// <summary>
    /// 执行缓存预热
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热成功的项数</returns>
    Task<int> ExecuteWarmupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行增量预热
    /// </summary>
    /// <param name="businessTags">业务标签过滤</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热成功的项数</returns>
    Task<int> ExecuteIncrementalWarmupAsync(string[]? businessTags = null, 
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 缓存策略相关方法
    
    /// <summary>
    /// 设置缓存策略
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="strategyType">策略类型</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetStrategyAsync(string key, CacheStrategyType strategyType, 
        TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取缓存策略
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存策略</returns>
    Task<CacheStrategyType?> GetStrategyAsync(string key, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 缓存监控相关方法
    
    /// <summary>
    /// 注册缓存过期回调
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="callback">回调函数</param>
    void RegisterExpirationCallback(string key, Func<string, Task> callback);
    
    /// <summary>
    /// 注册缓存预热完成回调
    /// </summary>
    /// <param name="callback">回调函数</param>
    void RegisterWarmupCompletedCallback(Action<int, int> callback);
    
    #endregion
}
