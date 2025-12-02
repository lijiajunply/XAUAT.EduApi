using StackExchange.Redis;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// Redis扩展方法
/// 用于简化Redis连接和操作
/// </summary>
public static class RedisExtensions
{
    /// <summary>
    /// 安全获取Redis数据库
    /// </summary>
    /// <param name="muxer">Redis连接多路复用器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="redisAvailable">Redis是否可用</param>
    /// <returns>Redis数据库实例，或null如果连接失败</returns>
    public static IDatabase? SafeGetDatabase(this IConnectionMultiplexer? muxer, ILogger? logger, out bool redisAvailable)
    {
        redisAvailable = false;
        if (muxer == null)
        {
            logger?.LogWarning("Redis连接多路复用器为空");
            return null;
        }

        try
        {
            var database = muxer.GetDatabase();
            redisAvailable = true;
            logger?.LogInformation("Redis连接成功");
            return database;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Redis连接失败");
            redisAvailable = false;
            return null;
        }
    }
    
    /// <summary>
    /// 安全获取Redis数据库
    /// </summary>
    /// <param name="muxer">Redis连接多路复用器</param>
    /// <param name="redisAvailable">Redis是否可用</param>
    /// <returns>Redis数据库实例，或null如果连接失败</returns>
    public static IDatabase? SafeGetDatabase(this IConnectionMultiplexer? muxer, out bool redisAvailable)
    {
        return muxer.SafeGetDatabase(null, out redisAvailable);
    }
    
    /// <summary>
    /// 安全执行Redis操作
    /// </summary>
    /// <param name="database">Redis数据库实例</param>
    /// <param name="redisAvailable">Redis是否可用</param>
    /// <param name="action">要执行的Redis操作</param>
    public static void SafeExecute(this IDatabase? database, bool redisAvailable, Action<IDatabase> action)
    {
        if (redisAvailable && database != null)
        {
            try
            {
                action(database);
            }
            catch (Exception ex)
            {
                // 记录Redis操作异常，但不影响主流程
                Console.WriteLine($"Redis操作异常: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 安全执行异步Redis操作
    /// </summary>
    /// <param name="database">Redis数据库实例</param>
    /// <param name="redisAvailable">Redis是否可用</param>
    /// <param name="func">要执行的异步Redis操作</param>
    /// <typeparam name="T">操作结果类型</typeparam>
    /// <returns>操作结果，或默认值如果操作失败</returns>
    public static async Task<T?> SafeExecuteAsync<T>(this IDatabase? database, bool redisAvailable, Func<IDatabase, Task<T>> func)
    {
        if (redisAvailable && database != null)
        {
            try
            {
                return await func(database);
            }
            catch (Exception ex)
            {
                // 记录Redis操作异常，但不影响主流程
                Console.WriteLine($"Redis操作异常: {ex.Message}");
            }
        }
        
        return default;
    }
}