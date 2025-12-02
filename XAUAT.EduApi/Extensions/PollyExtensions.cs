using Polly;
using Polly.Extensions.Http;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// Polly策略扩展方法
/// </summary>
public static class PollyExtensions
{
    /// <summary>
    /// 获取HTTP重试策略
    /// </summary>
    /// <returns>重试策略</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 处理瞬时HTTP错误（5xx和408）
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // 指数退避重试策略
    }
    
    /// <summary>
    /// 获取超时策略
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns>超时策略</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
    }
}
