using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Plugins;
using XAUAT.EduApi.ServiceDiscovery;

// 先创建builder对象
var builder = WebApplication.CreateBuilder(args);

// 启用顶级语句异步支持
// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

// 使用Serilog作为日志提供程序
builder.Host.UseSerilog();

// Add services to the container.

// 基础服务配置
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// 添加Prometheus监控服务
builder.Services.AddPrometheus();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                origin.EndsWith(".zeabur.app") || // 支持所有 zeabur.app 子域名
                origin.EndsWith(".xauat.site") || // 支持所有 xauat.site 子域名
                origin.StartsWith("http://localhost")) // 支持本地开发环境
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // 如果需要发送凭据（如cookies、认证头等）
    });
});

// 获取配置
var serviceConfig = ServiceConfiguration.FromConfiguration(builder.Configuration);

// 模块化服务注册
builder.Services.AddAllServices(serviceConfig);

var app = builder.Build();

// 初始化插件系统
var pluginManager = app.Services.GetRequiredService<PluginManager>();
pluginManager.Initialize();
pluginManager.LoadPlugins();

// 注册当前服务实例到服务注册中心
var serviceRegistry = app.Services.GetRequiredService<IServiceRegistry>();
var configuration = app.Configuration;

var serviceName = configuration.GetValue<string>("Service:Name") ?? "XAUAT.EduApi";
var instanceId = configuration.GetValue<string>("Service:InstanceId") ?? $"{serviceName}-{Guid.NewGuid()}";
var host = configuration.GetValue<string>("Service:Host") ?? "localhost";
var port = configuration.GetValue("Service:Port", 8080);
var isHttps = configuration.GetValue("Service:IsHttps", false);

var serviceInstance = new ServiceInstance
{
    ServiceName = serviceName,
    InstanceId = instanceId,
    Host = host,
    Port = port,
    IsHttps = isHttps,
    Metadata = new Dictionary<string, string>
    {
        { "Version", "1.0.0" },
        { "Environment", configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development" }
    },
    HealthCheckUrl = $"{(isHttps ? "https" : "http")}://{host}:{port}/health"
};

await serviceRegistry.RegisterAsync(serviceInstance);

// 配置中间件管道
app
    .UseErrorHandling()
    .UseAuthorization()
    .UseCustomCors()
    .UseMetricsCollection()
    .UsePrometheusMonitoring();

// 配置端点
app
    .ConfigureApiEndpoints()
    .ConfigureHealthChecks();

// Prometheus指标端点已通过UsePrometheusServer()配置，默认路径为/metrics

// 应用数据库迁移
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<IDbContextFactory<EduContext>>().CreateDbContext();
    var logger = services.GetRequiredService<ILogger<Program>>();

    var pending = context.Database.GetPendingMigrations();
    var enumerable = (pending as string[]) ?? pending.ToArray();

    if (enumerable.Length != 0)
    {
        logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", enumerable));
        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration error: {Message}", ex.Message);
            throw; // 让异常冒泡，方便定位问题
        }
    }
    else
    {
        logger.LogInformation("No pending migrations.");
    }

    await context.SaveChangesAsync();
    await context.DisposeAsync();
}

// 执行缓存预热（异步执行，不阻塞启动）
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting cache warmup...");
        var startTime = DateTime.Now;

        // 添加关键业务数据预热任务
        // 示例：添加学期信息预热
        cacheService.AddWarmupTask(new CacheWarmupItem
        {
            Key = "cache:semesters",
            ValueFactory = () =>
                Task.FromResult<object>(
                    new List<string> { "2025-2026-1", "2025-2026-2", "2024-2025-1", "2024-2025-2" }),
            Expiration = TimeSpan.FromDays(7),
            Priority = 10,
            BusinessTags = ["core", "semester"]
        });

        // 执行预热
        var successCount = await cacheService.ExecuteWarmupAsync();

        var elapsed = DateTime.Now - startTime;
        logger.LogInformation(
            "Cache warmup completed successfully. Warmed up {SuccessCount} items in {ElapsedMilliseconds}ms",
            successCount, elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during cache warmup: {Message}", ex.Message);
    }
});

// 注册应用程序停止事件，用于清理资源
AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
    // 注销服务实例
    await serviceRegistry.DeregisterAsync(serviceName, instanceId);

    // 停止所有插件
    pluginManager.StopAllPlugins();
    pluginManager.UnloadAllPlugins();

    // 确保所有日志都被正确写入
    Log.CloseAndFlush();
};

// 注册应用程序取消事件
var cts = new CancellationTokenSource();
app.Lifetime.ApplicationStopping.Register(() => { cts.Cancel(); });

app.Run();

// 确保所有日志都被正确写入
Log.CloseAndFlush();