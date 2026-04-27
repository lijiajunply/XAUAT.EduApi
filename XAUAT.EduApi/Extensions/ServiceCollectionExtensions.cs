using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.ServiceDiscovery;
using XAUAT.EduApi.HealthChecks;
using XAUAT.EduApi.Interfaces;
using XAUAT.EduApi.Queues;
using XAUAT.EduApi.Repos;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// 服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">服务集合</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// 注册Prometheus监控服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddPrometheus()
        {
            // 这里可以添加Prometheus相关的服务注册
            // 例如：services.AddPrometheusMetrics();
            return services;
        }

        /// <summary>
        /// 注册数据库服务
        /// </summary>
        /// <param name="sqlConnectionString">SQL连接字符串</param>
        /// <returns>服务集合</returns>
        public IServiceCollection AddDatabaseServices(string? sqlConnectionString)
        {
            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                services.AddDbContextFactory<EduContext>(opt =>
                    opt.UseSqlite("Data Source=Data.db",
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            }
            else
            {
                services.AddDbContextFactory<EduContext>(opt =>
                    opt.UseNpgsql(sqlConnectionString,
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            }

            return services;
        }

        /// <summary>
        /// 注册Redis服务
        /// </summary>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <returns>服务集合</returns>
        public IServiceCollection AddRedisServices(string? redisConnectionString)
        {
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
            }

            return services;
        }

        /// <summary>
        /// 注册仓库服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddRepositoryServices()
        {
            services.AddScoped<IScoreRepository, ScoreRepository>();

            return services;
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddBusinessServices()
        {
            services.AddScoped<ICodeService, CodeService>();
            services.AddScoped<ILoginService, SSOLoginService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IProgramService, ProgramService>();
            services.AddScoped<IInfoService, InfoService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IScoreService, ScoreService>();
            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<ICookieCodeService, CookieCodeService>();
            services.AddSingleton<IScorePersistenceQueue, ChannelScorePersistenceQueue>();
            services.AddHostedService<ScorePersistenceBackgroundService>();

            // 添加监控服务
            services.AddSingleton<IMonitoringService, MonitoringService>();

            return services;
        }

        /// <summary>
        /// 注册HTTP客户端服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddHttpClientServices()
        {
            // 配置默认的HttpClient（不跳过SSL验证）
            services.AddHttpClient("DefaultClient")
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10); // 设置默认超时时间为10秒
                })
                .AddPolicyHandler(PollyExtensions.GetRetryPolicy()); // 添加重试策略

            // 配置专门用于BusController的HttpClient（跳过SSL验证）
            services.AddHttpClient("BusClient")
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15); // 设置超时时间为10秒
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                    MaxConnectionsPerServer = 100, // 设置每个服务器的最大连接数
                    AllowAutoRedirect = true, // 允许自动重定向
                    UseCookies = true // 使用Cookie
                })
                .AddPolicyHandler(PollyExtensions.GetRetryPolicy()); // 添加重试策略

            // 配置专门用于PaymentService的HttpClient（跳过SSL验证）
            services.AddHttpClient("PaymentClient")
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15); // 设置超时时间为15秒
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                    MaxConnectionsPerServer = 100, // 设置每个服务器的最大连接数
                    AllowAutoRedirect = true, // 允许自动重定向
                    UseCookies = true // 使用Cookie
                })
                .AddPolicyHandler(PollyExtensions.GetRetryPolicy()); // 添加重试策略

            // 配置专门用于外部API的HttpClient
            services.AddHttpClient("ExternalApiClient")
                .ConfigureHttpClient(client => { client.Timeout = TimeSpan.FromSeconds(10); })
                .AddPolicyHandler(PollyExtensions.GetRetryPolicy());

            return services;
        }

        /// <summary>
        /// 注册健康检查服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddHealthCheckServices()
        {
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database")
                .AddCheck<RedisHealthCheck>("redis");

            return services;
        }

        /// <summary>
        /// 注册服务发现服务
        /// </summary>
        /// <returns>服务集合</returns>
        public IServiceCollection AddServiceDiscoveryServices()
        {
            // 注册服务注册中心
            services.TryAddSingleton<IServiceRegistry, InMemoryServiceRegistry>();

            return services;
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        /// <param name="sqlConnectionString">SQL连接字符串</param>
        /// <param name="redisConnectionString">Redis连接字符串</param>
        /// <returns>服务集合</returns>
        public IServiceCollection AddAllServices(string? sqlConnectionString, string? redisConnectionString)
        {
            return services.AddAllServices(new ServiceConfiguration
            {
                SqlConnectionString = sqlConnectionString,
                RedisConnectionString = redisConnectionString
            });
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        /// <param name="configuration">服务配置</param>
        /// <returns>服务集合</returns>
        public IServiceCollection AddAllServices(ServiceConfiguration configuration)
        {
            var serviceCollection = services
                .AddDatabaseServices(configuration.SqlConnectionString)
                .AddRedisServices(configuration.RedisConnectionString)
                .AddCacheServices() // 添加缓存服务
                .AddRepositoryServices()
                .AddBusinessServices()
                .AddHttpClientServices()
                // 添加新架构服务
                .AddServiceDiscoveryServices();

            if (configuration.EnablePrometheus)
            {
                serviceCollection.AddPrometheus();
            }

            if (configuration.EnableHealthChecks)
            {
                serviceCollection.AddHealthCheckServices();
            }

            return serviceCollection;
        }
    }
}
