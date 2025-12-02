using XAUAT.EduApi.Middlewares;
using Prometheus.Client.AspNetCore;
using Scalar.AspNetCore;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// 应用程序构建器扩展方法
/// 用于模块化注册中间件
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用错误处理中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        app.UseErrorHandlingMiddleware();
        return app;
    }
    
    /// <summary>
    /// 使用CORS中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
    {
        app.UseCors();
        return app;
    }
    
    /// <summary>
    /// 使用指标收集中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseMetricsCollection(this IApplicationBuilder app)
    {
        app.UseMiddleware<MetricsMiddleware>();
        return app;
    }
    
    /// <summary>
    /// 使用Prometheus监控中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UsePrometheusMonitoring(this IApplicationBuilder app)
    {
        app.UsePrometheusServer();
        return app;
    }
    
    /// <summary>
    /// 配置API端点
    /// </summary>
    /// <param name="app">应用程序</param>
    /// <returns>应用程序</returns>
    public static WebApplication ConfigureApiEndpoints(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapControllers();
        app.MapScalarApiReference();
        return app;
    }
    
    /// <summary>
    /// 配置健康检查端点
    /// </summary>
    /// <param name="app">应用程序</param>
    /// <returns>应用程序</returns>
    public static WebApplication ConfigureHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health")
            .WithDisplayName("健康检查")
            .WithDescription("检查服务的健康状态");
        return app;
    }
    
    /// <summary>
    /// 配置完整的中间件管道
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder ConfigureMiddlewarePipeline(this IApplicationBuilder app)
    {
        return app
            .UseErrorHandling()
            .UseAuthorization()
            .UseCustomCors()
            .UseMetricsCollection()
            .UsePrometheusMonitoring();
    }
    
    /// <summary>
    /// 配置完整的应用程序
    /// </summary>
    /// <param name="app">应用程序</param>
    /// <returns>应用程序</returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        // 配置中间件管道
        app.ConfigureMiddlewarePipeline();
        
        // 配置端点
        app.ConfigureApiEndpoints();
        app.ConfigureHealthChecks();
        
        return app;
    }
}