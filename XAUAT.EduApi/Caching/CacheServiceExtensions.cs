using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace XAUAT.EduApi.Caching;

/// <summary>
/// 缓存服务注册扩展
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheServices(this IServiceCollection services)
    {
        // 注册缓存配置选项
        services.AddOptions<CacheOptions>()
            .Configure(options =>
            {
                // 默认配置
                options.KeyPrefix = "EduApi:";
                options.DefaultExpiration = TimeSpan.FromHours(1);
                options.StrategyType = CacheStrategyType.Hybrid;
                options.LocalCacheMaxSize = 1000;
                options.PreUpdateThreshold = TimeSpan.FromMinutes(5);
                options.RefreshInterval = TimeSpan.FromMinutes(1);
            });
        
        // 注册缓存服务
        services.AddSingleton<ICacheService, CacheService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheServices(this IServiceCollection services, Action<CacheOptions> configureOptions)
    {
        // 注册缓存配置选项
        services.AddOptions<CacheOptions>()
            .Configure(configureOptions);
        
        // 注册缓存服务
        services.AddSingleton<ICacheService, CacheService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheServices(this IServiceCollection services, string sectionName)
    {
        // 从配置中绑定缓存选项
        services.Configure<CacheOptions>(options =>
        {
            // 默认配置
            options.KeyPrefix = "EduApi:";
            options.DefaultExpiration = TimeSpan.FromHours(1);
            options.StrategyType = CacheStrategyType.Hybrid;
            options.LocalCacheMaxSize = 1000;
            options.PreUpdateThreshold = TimeSpan.FromMinutes(5);
            options.RefreshInterval = TimeSpan.FromMinutes(1);
        });
        
        // 注册缓存服务
        services.AddSingleton<ICacheService, CacheService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加缓存服务，支持从配置文件读取配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheServices(this IServiceCollection services, IConfiguration configuration, string sectionName = "Cache")
    {
        // 从配置中绑定缓存选项
        services.Configure<CacheOptions>(configuration.GetSection(sectionName));
        
        // 注册缓存服务
        services.AddSingleton<ICacheService, CacheService>();
        
        return services;
    }
}
