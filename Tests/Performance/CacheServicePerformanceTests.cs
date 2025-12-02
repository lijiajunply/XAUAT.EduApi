using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XAUAT.EduApi.Caching;

namespace Tests.Performance;

/// <summary>
/// 缓存服务性能测试
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
public class CacheServicePerformanceTests
{
    private ICacheService? _cacheService;
    private const string TestKey = "test:key";
    private const string TestValue = "test-value-1234567890";
    private const int ConcurrentCount = 100;
    private const int IterationCount = 1000;
    
    [GlobalSetup]
    public void Setup()
    {
        // 创建服务提供程序
        var services = new ServiceCollection();
        
        // 注册缓存服务
        services.AddCacheServices(options =>
        {
            options.DefaultExpiration = TimeSpan.FromHours(1);
            options.StrategyType = CacheStrategyType.Hybrid;
            options.LocalCacheMaxSize = 1000;
        });
        
        // 注册日志服务
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        
        // 构建服务提供程序
        var serviceProvider = services.BuildServiceProvider();
        
        // 获取缓存服务
        _cacheService = serviceProvider.GetRequiredService<ICacheService>();
    }
    
    [Benchmark]
    public async Task Cache_SetAsync()
    {
        await _cacheService!.SetAsync(TestKey, TestValue);
    }
    
    [Benchmark]
    public async Task Cache_GetAsync()
    {
        await _cacheService!.GetAsync<string>(TestKey);
    }
    
    [Benchmark]
    public async Task Cache_GetOrCreateAsync()
    {
        await _cacheService!.GetOrCreateAsync(TestKey, () => Task.FromResult(TestValue));
    }
    
    [Benchmark]
    public async Task Cache_Concurrent_GetSet()
    {
        var tasks = new List<Task>();
        
        for (var i = 0; i < ConcurrentCount; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                var key = $"concurrent:{taskId}";
                await _cacheService!.SetAsync(key, $"value:{taskId}");
                await _cacheService!.GetAsync<string>(key);
            }));
        }
        
        await Task.WhenAll(tasks);
    }
    
    [Benchmark]
    public async Task Cache_MultiLevel_Performance()
    {
        // 先设置缓存，确保在本地和分布式缓存中都存在
        await _cacheService!.SetAsync(TestKey, TestValue);
        
        // 多次获取，测试多级缓存性能
        for (var i = 0; i < IterationCount; i++)
        {
            await _cacheService!.GetAsync<string>(TestKey);
        }
    }
    
    [Benchmark]
    public async Task Cache_Warmup_Performance()
    {
        // 测试缓存预热性能
        for (var i = 0; i < 100; i++)
        {
            _cacheService!.AddWarmupTask(new CacheWarmupItem
            {
                Key = $"warmup:{i}",
                ValueFactory = async () => await Task.FromResult($"warmup-value:{i}"),
                Priority = 5
            });
        }
        
        await _cacheService!.ExecuteWarmupAsync();
    }
    
    [Benchmark]
    public async Task WithoutCache_Performance()
    {
        // 模拟没有缓存的情况，每次都生成新值
        for (var i = 0; i < IterationCount; i++)
        {
            // 模拟耗时操作
            await Task.Delay(1); // 1ms延迟模拟数据库查询
            var _ = TestValue;
        }
    }
    
    [Benchmark]
    public async Task Cache_HitRatio_Test()
    {
        // 测试缓存命中率
        var hitKeys = new string[800];
        var missKeys = new string[200];
        
        // 设置80%的键到缓存中
        for (var i = 0; i < 800; i++)
        {
            var key = $"hit:{i}";
            hitKeys[i] = key;
            await _cacheService!.SetAsync(key, $"value:{i}");
        }
        
        // 20%的键不设置到缓存中
        for (var i = 0; i < 200; i++)
        {
            missKeys[i] = $"miss:{i}";
        }
        
        // 随机访问这些键
        var random = new Random();
        var allKeys = hitKeys.Concat(missKeys).ToArray();
        
        for (var i = 0; i < IterationCount; i++)
        {
            var index = random.Next(0, allKeys.Length);
            await _cacheService!.GetAsync<string>(allKeys[index]);
        }
    }
    
    [Benchmark]
    public async Task LocalCache_Vs_RedisCache()
    {
        // 测试本地缓存 vs Redis缓存性能
        
        // 本地缓存测试
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < IterationCount; i++)
        {
            var key = $"local:{i}";
            await _cacheService!.SetAsync(key, $"value:{i}", level: CacheLevel.Local);
            await _cacheService!.GetAsync<string>(key);
        }
        var localTime = stopwatch.Elapsed;
        
        // Redis缓存测试
        stopwatch.Restart();
        for (var i = 0; i < IterationCount; i++)
        {
            var key = $"redis:{i}";
            await _cacheService!.SetAsync(key, $"value:{i}", level: CacheLevel.Distributed);
            await _cacheService!.GetAsync<string>(key);
        }
        var redisTime = stopwatch.Elapsed;
        
        // 记录结果（用于输出）
        Console.WriteLine($"Local Cache Time: {localTime.TotalMilliseconds}ms");
        Console.WriteLine($"Redis Cache Time: {redisTime.TotalMilliseconds}ms");
    }
    
    [Benchmark]
    public async Task Cache_Statistics()
    {
        // 测试获取缓存统计信息
        await _cacheService!.GetStatisticsAsync();
    }
    
    [Benchmark]
    public async Task Cache_RemoveAsync()
    {
        // 测试移除缓存
        await _cacheService!.RemoveAsync(TestKey);
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        // 清除所有缓存项
        _cacheService?.ClearAsync().Wait();
    }
}
