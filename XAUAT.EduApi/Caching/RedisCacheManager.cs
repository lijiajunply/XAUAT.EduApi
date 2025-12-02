using System.Runtime.CompilerServices;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XAUAT.EduApi.Caching;

/// <summary>
/// Redis分布式缓存管理器
/// </summary>
internal class RedisCacheManager
{
    private readonly IDatabase? _redis;
    private readonly IConnectionMultiplexer _connection;
    private readonly CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    // 统计信息
    private long _hitCount;
    private long _missCount;
    private long _errorCount;

    /// <summary>
    /// Redis是否可用
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connection">Redis连接多路复用器</param>
    /// <param name="options">缓存配置选项</param>
    public RedisCacheManager(IConnectionMultiplexer connection, CacheOptions options)
    {
        _connection = connection;
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 32
        };

        try
        {
            _redis = connection.GetDatabase();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            Interlocked.Increment(ref _errorCount);
        }

        // 订阅连接事件
        connection.ConnectionFailed += (_, _) => IsAvailable = false;
        connection.ConnectionRestored += (_, _) => IsAvailable = true;
    }

    /// <summary>
    /// 获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存项，如果不存在则返回null</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            Interlocked.Increment(ref _missCount);
            return default;
        }

        try
        {
            var fullKey = GetFullKey(key);
            var redisValue = await _redis.StringGetAsync(fullKey, CommandFlags.PreferReplica);

            if (redisValue.IsNull)
            {
                Interlocked.Increment(ref _missCount);
                return default;
            }

            var cacheItem = JsonSerializer.Deserialize<CacheItem<T>>(redisValue.ToString(), _jsonOptions);
            if (cacheItem == null)
            {
                Interlocked.Increment(ref _missCount);
                return default;
            }

            // 检查是否过期
            if (cacheItem.IsExpired)
            {
                await RemoveAsync(key, cancellationToken);
                Interlocked.Increment(ref _missCount);
                return default;
            }

            // 更新访问信息
            cacheItem.HitCount++;
            cacheItem.LastAccessTime = DateTime.Now;

            // 异步更新，不阻塞主流程
            _ = UpdateCacheItemAsync(cacheItem);

            Interlocked.Increment(ref _hitCount);
            return cacheItem.Value;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            Interlocked.Increment(ref _missCount);
            IsAvailable = false;
            return default;
        }
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="businessPriority">业务优先级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    public async Task<bool> SetAsync<T>(string key, T? value, TimeSpan? expiration = null, int businessPriority = 5,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            return false;
        }

        if (value == null)
        {
            return await RemoveAsync(key, cancellationToken);
        }

        try
        {
            var fullKey = GetFullKey(key);
            var actualExpiration = expiration ?? _options.DefaultExpiration;
            var now = DateTime.Now;

            var cacheItem = new CacheItem<T>
            {
                Key = key,
                Value = value,
                CreatedTime = now,
                ExpirationTime = now.Add(actualExpiration),
                HitCount = 0,
                LastAccessTime = now,
                Level = CacheLevel.Distributed,
                BusinessPriority = businessPriority
            };

            var json = JsonSerializer.Serialize(cacheItem, _jsonOptions);

            var result = await _redis.StringSetAsync(fullKey, json, actualExpiration, When.Always,
                CommandFlags.FireAndForget);

            return result;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 移除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            return false;
        }

        try
        {
            var fullKey = GetFullKey(key);
            var result = await _redis.KeyDeleteAsync(fullKey, CommandFlags.FireAndForget);
            return result;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            return false;
        }

        try
        {
            var fullKey = GetFullKey(key);
            var exists = await _redis.KeyExistsAsync(fullKey, CommandFlags.PreferReplica);

            if (exists)
            {
                // 额外检查过期时间
                var ttl = await _redis.KeyTimeToLiveAsync(fullKey);
                if (ttl is { TotalSeconds: <= 0 })
                {
                    await RemoveAsync(key, cancellationToken);
                    return false;
                }
            }

            return exists;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 清除所有缓存项
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否清除成功</returns>
    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            return false;
        }

        try
        {
            var pattern = GetFullKey("*");
            var endpoints = _connection.GetEndPoints();

            foreach (var endpoint in endpoints)
            {
                var server = _connection.GetServer(endpoint);
                if (!server.IsConnected)
                {
                    continue;
                }

                // 使用SCAN命令代替KEYS，避免阻塞Redis
                await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 1000)
                                   .WithCancellation(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _redis.KeyDeleteAsync(key, CommandFlags.FireAndForget);
                }
            }

            return true;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 批量获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="keys">缓存键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存项字典</returns>
    public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();

        var enumerable = keys as string[] ?? keys.ToArray();
        if (!IsAvailable || _redis == null || enumerable.Length == 0)
        {
            return result;
        }

        try
        {
            var keyArray = enumerable.ToArray();
            var tasks = new List<Task<(string Key, T? Value)>>();

            // 并行获取所有键
            foreach (var key in keyArray)
            {
                tasks.Add(GetSingleAsync<T>(key, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var (key, value) in results)
            {
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return result;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return result;
        }
    }

    /// <summary>
    /// 批量设置缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="items">缓存项字典</param>
    /// <param name="expiration">过期时间</param>
    /// <param name="businessPriority">业务优先级</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否设置成功</returns>
    public async Task<bool> SetManyAsync<T>(IDictionary<string, T?> items, TimeSpan? expiration = null,
        int businessPriority = 5, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null || !items.Any())
        {
            return false;
        }

        try
        {
            var actualExpiration = expiration ?? _options.DefaultExpiration;
            var now = DateTime.Now;

            // 使用事务批量设置
            var batch = _redis.CreateBatch();
            var tasks = new List<Task>();

            foreach (var (key, value) in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (value == null)
                {
                    var removeTask = batch.KeyDeleteAsync(GetFullKey(key), CommandFlags.FireAndForget);
                    tasks.Add(removeTask);
                    continue;
                }

                var cacheItem = new CacheItem<T>
                {
                    Key = key,
                    Value = value,
                    CreatedTime = now,
                    ExpirationTime = now.Add(actualExpiration),
                    HitCount = 0,
                    LastAccessTime = now,
                    Level = CacheLevel.Distributed,
                    BusinessPriority = businessPriority
                };

                var json = JsonSerializer.Serialize(cacheItem, _jsonOptions);
                var setTask = batch.StringSetAsync(GetFullKey(key), json, actualExpiration, When.Always,
                    CommandFlags.FireAndForget);
                tasks.Add(setTask);
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            return true;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 批量移除缓存项
    /// </summary>
    /// <param name="keys">缓存键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否移除成功</returns>
    public async Task<bool> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var enumerable = keys as string[] ?? keys.ToArray();
        if (!IsAvailable || _redis == null || !enumerable.Any())
        {
            return false;
        }

        try
        {
            var keyArray = enumerable.ToArray();

            // 使用事务批量删除
            var batch = _redis.CreateBatch();
            var tasks = new List<Task>();

            foreach (var key in keyArray)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var task = batch.KeyDeleteAsync(GetFullKey(key), CommandFlags.FireAndForget);
                tasks.Add(task);
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            return true;
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
            return false;
        }
    }

    /// <summary>
    /// 搜索缓存键
    /// </summary>
    /// <param name="pattern">键匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的缓存键列表</returns>
    public IAsyncEnumerable<string> SearchKeysAsync(string pattern,
        CancellationToken cancellationToken = default)
    {
        return SearchKeysInternalAsync(pattern, cancellationToken);
    }

    /// <summary>
    /// 搜索缓存键内部实现
    /// </summary>
    /// <param name="pattern">键匹配模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的缓存键列表</returns>
    private async IAsyncEnumerable<string> SearchKeysInternalAsync(string pattern,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _redis == null)
        {
            yield break;
        }

        var fullPattern = GetFullKey(pattern);
        var endpoints = _connection.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IServer? server;

            // 尝试获取服务器，单独处理可能的异常
            try
            {
                server = _connection.GetServer(endpoint);
            }
            catch
            {
                Interlocked.Increment(ref _errorCount);
                continue;
            }

            if (server is not { IsConnected: true })
            {
                continue;
            }

            // 遍历键，不使用try-catch包裹yield return
            await foreach (var key in server.KeysAsync(pattern: fullPattern, pageSize: 1000)
                               .WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 移除键前缀
                var originalKey = key.ToString().Substring(_options.KeyPrefix.Length);
                yield return originalKey;
            }
        }
    }

    /// <summary>
    /// 获取Redis缓存统计信息
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public RedisCacheStatistics GetStatistics()
    {
        try
        {
            var hitCount = Interlocked.Read(ref _hitCount);
            var missCount = Interlocked.Read(ref _missCount);
            var errorCount = Interlocked.Read(ref _errorCount);

            return new RedisCacheStatistics
            {
                IsAvailable = IsAvailable,
                HitCount = hitCount,
                MissCount = missCount,
                ErrorCount = errorCount,
                HitRate = hitCount + missCount > 0 ? (double)hitCount / (hitCount + missCount) : 0,
                TotalRequests = hitCount + missCount
            };
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            return new RedisCacheStatistics
            {
                IsAvailable = false,
                ErrorCount = Interlocked.Read(ref _errorCount) + 1
            };
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 单个键获取辅助方法
    /// </summary>
    private async Task<(string Key, T? Value)> GetSingleAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await GetAsync<T>(key, cancellationToken);
        return (key, value);
    }

    /// <summary>
    /// 更新缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="cacheItem">缓存项</param>
    private async Task UpdateCacheItemAsync<T>(CacheItem<T> cacheItem)
    {
        if (!IsAvailable || _redis == null)
        {
            return;
        }

        try
        {
            var fullKey = GetFullKey(cacheItem.Key);
            var json = JsonSerializer.Serialize(cacheItem, _jsonOptions);

            // 使用现有过期时间
            var ttl = cacheItem.RemainingTime;
            if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
            {
                await _redis.StringSetAsync(fullKey, json, ttl.Value, When.Exists, CommandFlags.FireAndForget);
            }
        }
        catch
        {
            Interlocked.Increment(ref _errorCount);
            IsAvailable = false;
        }
    }

    /// <summary>
    /// 获取带前缀的完整缓存键
    /// </summary>
    /// <param name="key">原始缓存键</param>
    /// <returns>带前缀的完整缓存键</returns>
    private string GetFullKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }

    #endregion

    /// <summary>
    /// Redis缓存统计信息
    /// </summary>
    [Serializable]
    public class RedisCacheStatistics
    {
        /// <summary>
        /// Redis是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 命中次数
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// 未命中次数
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// 错误次数
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// 总请求次数
        /// </summary>
        public long TotalRequests { get; set; }
    }
}