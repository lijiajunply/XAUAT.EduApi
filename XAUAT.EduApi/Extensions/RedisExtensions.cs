using Newtonsoft.Json;
using StackExchange.Redis;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// Redis扩展方法
/// 用于简化Redis连接和操作
/// </summary>
public static class RedisExtensions
{
    /// <param name="muxer">Redis连接多路复用器</param>
    extension(IConnectionMultiplexer? muxer)
    {
        /// <summary>
        /// 安全获取Redis数据库
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <returns>Redis数据库实例，或null如果连接失败</returns>
        public IDatabase? SafeGetDatabase(ILogger? logger, out bool redisAvailable)
        {
            redisAvailable = false;
            if (muxer == null)
            {
                logger?.LogWarning("Redis未配置，将使用数据库作为缓存");
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
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <returns>Redis数据库实例，或null如果连接失败</returns>
        public IDatabase? SafeGetDatabase(out bool redisAvailable)
        {
            return muxer.SafeGetDatabase(null, out redisAvailable);
        }
    }

    /// <param name="database">Redis数据库实例</param>
    extension(IDatabase? database)
    {
        /// <summary>
        /// 安全执行Redis操作
        /// </summary>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="action">要执行的Redis操作</param>
        /// <param name="logger">日志记录器（可选）</param>
        public void SafeExecute(bool redisAvailable, Action<IDatabase> action, ILogger? logger = null)
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
                    logger?.LogWarning(ex, "Redis操作异常: {Message}", ex.Message);
                }
            }
        }

        /// <summary>
        /// 安全执行异步Redis操作
        /// </summary>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="func">要执行的异步Redis操作</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <typeparam name="T">操作结果类型</typeparam>
        /// <returns>操作结果，或默认值如果操作失败</returns>
        public async Task<T?> SafeExecuteAsync<T>(bool redisAvailable,
            Func<IDatabase, Task<T>> func, ILogger? logger = null)
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
                    logger?.LogWarning(ex, "Redis操作异常: {Message}", ex.Message);
                }
            }

            return default;
        }

        /// <summary>
        /// 从Redis获取缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>缓存的数据，如果不存在或出错则返回default</returns>
        public async Task<T?> GetCacheAsync<T>(bool redisAvailable, string cacheKey, ILogger? logger = null)
            where T : class
        {
            if (!redisAvailable || database == null)
            {
                return null;
            }

            try
            {
                var redisResult = await database.StringGetAsync(cacheKey).ConfigureAwait(false);
                if (redisResult.HasValue)
                {
                    var cached = JsonConvert.DeserializeObject<T>(redisResult.ToString());
                    if (cached != null)
                    {
                        logger?.LogInformation("已从Redis缓存获取数据，键: {CacheKey}", cacheKey);
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis缓存读取失败，键: {CacheKey}", cacheKey);
            }

            return null;
        }

        /// <summary>
        /// 将数据保存到Redis缓存
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="data">要缓存的数据</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>是否成功保存</returns>
        public async Task<bool> SetCacheAsync<T>(bool redisAvailable, string cacheKey, T data, TimeSpan expiry, ILogger? logger = null)
        {
            if (!redisAvailable || database == null || data == null)
            {
                return false;
            }

            try
            {
                await database.StringSetAsync(cacheKey, JsonConvert.SerializeObject(data), expiry).ConfigureAwait(false);
                logger?.LogInformation("数据已缓存到Redis，键: {CacheKey}", cacheKey);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis缓存写入失败，键: {CacheKey}", cacheKey);
                return false;
            }
        }

        /// <summary>
        /// 从Redis获取字符串缓存
        /// </summary>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>缓存的字符串，如果不存在或出错则返回null</returns>
        public async Task<string?> GetStringCacheAsync(bool redisAvailable, string cacheKey, ILogger? logger = null)
        {
            if (!redisAvailable || database == null)
            {
                return null;
            }

            try
            {
                var redisResult = await database.StringGetAsync(cacheKey).ConfigureAwait(false);
                if (redisResult is { HasValue: true, IsNullOrEmpty: false })
                {
                    logger?.LogInformation("已从Redis缓存获取字符串，键: {CacheKey}", cacheKey);
                    return redisResult.ToString();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis缓存读取失败，键: {CacheKey}", cacheKey);
            }

            return null;
        }

        /// <summary>
        /// 将字符串保存到Redis缓存
        /// </summary>
        /// <param name="redisAvailable">Redis是否可用</param>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="value">要缓存的字符串</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>是否成功保存</returns>
        public async Task<bool> SetStringCacheAsync(bool redisAvailable, string cacheKey, string value, TimeSpan expiry, ILogger? logger = null)
        {
            if (!redisAvailable || database == null || string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                await database.StringSetAsync(cacheKey, value, expiry).ConfigureAwait(false);
                logger?.LogInformation("字符串已缓存到Redis，键: {CacheKey}", cacheKey);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis缓存写入失败，键: {CacheKey}", cacheKey);
                return false;
            }
        }
    }
}

/// <summary>
/// 缓存键生成器
/// 用于统一生成缓存键，避免键冲突
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "eduapi";

    /// <summary>
    /// 生成成绩缓存键
    /// </summary>
    public static string Scores(string studentId, string semester) => $"{Prefix}:scores:{studentId}:{semester}";

    /// <summary>
    /// 生成学期结果缓存键
    /// </summary>
    public static string SemesterResult(string? studentId) => $"{Prefix}:semester_result:{studentId}";

    /// <summary>
    /// 生成当前学期缓存键
    /// </summary>
    public static string ThisSemester => $"{Prefix}:thisSemester";

    /// <summary>
    /// 生成考试安排缓存键
    /// </summary>
    public static string ExamArrangement(string? id) => $"{Prefix}:exam_arrangement:{id}";

    /// <summary>
    /// 生成支付Token缓存键
    /// </summary>
    public static string PaymentToken(string cardNum) => $"{Prefix}:payment:{cardNum}";

    /// <summary>
    /// 生成支付列表缓存键
    /// </summary>
    public static string PaymentList(string cardNum) => $"{Prefix}:paymentList:{cardNum}";

    /// <summary>
    /// 生成电子账户余额缓存键
    /// </summary>
    public static string ElectronicAccount(string cardNum) => $"{Prefix}:ele_acc:{cardNum}";

    /// <summary>
    /// 生成培养方案缓存键
    /// </summary>
    public static string TrainProgram(string id) => $"{Prefix}:train_program:{id}";

    /// <summary>
    /// 生成校车缓存键
    /// </summary>
    public static string Bus(string time) => $"{Prefix}:bus:{time}";

    /// <summary>
    /// 生成新校车数据缓存键
    /// </summary>
    public static string BusNewData(string time) => $"{Prefix}:bus_new_data:{time}";
}
