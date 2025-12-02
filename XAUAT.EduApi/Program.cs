using EduApi.Data;
using Microsoft.EntityFrameworkCore;
using Prometheus.Client.AspNetCore;
using Scalar.AspNetCore;
using Serilog;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Middlewares;

// 先创建builder对象
var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// 添加Prometheus监控服务
builder.Services.AddPrometheus();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

// 获取配置
var serviceConfig = ServiceConfiguration.FromConfiguration(builder.Configuration);

// 模块化服务注册
builder.Services.AddAllServices(serviceConfig);

var app = builder.Build();

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
    var enumerable = pending as string[] ?? pending.ToArray();

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

app.Run();

// 确保所有日志都被正确写入
Log.CloseAndFlush();