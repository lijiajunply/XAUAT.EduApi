using System.Net;
using System.Text.Json;

namespace XAUAT.EduApi.Middlewares;

/// <summary>
/// 错误处理中间件
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="logger">日志记录器</param>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 执行中间件
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="exception">异常</param>
    /// <returns>任务</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "发生未处理的异常: {Path}", context.Request.Path);

        // 设置响应状态码
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // 设置响应头
        context.Response.ContentType = "application/json";

        // 构建错误响应
        var errorResponse = new
        {
            context.Response.StatusCode,
            Message = "服务器内部错误，请联系管理员",
            Details = exception.Message,
            context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        // 序列化错误响应
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        // 写入响应
        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// 错误处理中间件扩展方法
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// 使用错误处理中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}