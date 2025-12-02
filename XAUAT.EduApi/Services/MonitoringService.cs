using Prometheus.Client;

namespace XAUAT.EduApi.Services;

/// <summary>
/// 监控服务实现
/// 使用Prometheus.Client收集和暴露业务监控指标
/// </summary>
public class MonitoringService : IMonitoringService
{
    // API相关指标
    private readonly ICounter _apiCallCounter;
    private readonly ICounter _apiErrorCounter;
    private readonly IHistogram _apiRequestDurationHistogram;
    
    // 数据库相关指标
    private readonly ICounter _databaseQueryCounter;
    private readonly ICounter _databaseErrorCounter;
    private readonly IHistogram _databaseQueryDurationHistogram;
    
    // 外部API相关指标
    private readonly ICounter _externalApiCallCounter;
    private readonly ICounter _externalApiErrorCounter;
    private readonly IHistogram _externalApiDurationHistogram;
    
    // 缓存相关指标
    private readonly ICounter _cacheHitCounter;
    private readonly ICounter _cacheMissCounter;
    private readonly ICounter _cacheErrorCounter;
    
    // 业务操作相关指标
    private readonly ICounter _businessOperationCounter;
    private readonly ICounter _businessErrorCounter;
    private readonly IHistogram _businessOperationDurationHistogram;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MonitoringService()
    {
        // API相关指标
        _apiCallCounter = Metrics.DefaultFactory.CreateCounter(
            "api_calls_total",
            "Total number of API calls");
        
        _apiErrorCounter = Metrics.DefaultFactory.CreateCounter(
            "api_errors_total",
            "Total number of API errors");
        
        _apiRequestDurationHistogram = Metrics.DefaultFactory.CreateHistogram(
            "api_request_duration_seconds",
            "Duration of API requests in seconds",
            buckets: new[] { 0.001, 0.01, 0.1, 0.5, 1, 2, 5, 10 });
        
        // 数据库相关指标
        _databaseQueryCounter = Metrics.DefaultFactory.CreateCounter(
            "database_queries_total",
            "Total number of database queries");
        
        _databaseErrorCounter = Metrics.DefaultFactory.CreateCounter(
            "database_errors_total",
            "Total number of database errors");
        
        _databaseQueryDurationHistogram = Metrics.DefaultFactory.CreateHistogram(
            "database_query_duration_seconds",
            "Duration of database queries in seconds",
            buckets: new[] { 0.001, 0.01, 0.1, 0.5, 1, 2, 5, 10 });
        
        // 外部API相关指标
        _externalApiCallCounter = Metrics.DefaultFactory.CreateCounter(
            "external_api_calls_total",
            "Total number of external API calls");
        
        _externalApiErrorCounter = Metrics.DefaultFactory.CreateCounter(
            "external_api_errors_total",
            "Total number of external API errors");
        
        _externalApiDurationHistogram = Metrics.DefaultFactory.CreateHistogram(
            "external_api_duration_seconds",
            "Duration of external API calls in seconds",
            buckets: new[] { 0.001, 0.01, 0.1, 0.5, 1, 2, 5, 10, 30 });
        
        // 缓存相关指标
        _cacheHitCounter = Metrics.DefaultFactory.CreateCounter(
            "cache_hits_total",
            "Total number of cache hits");
        
        _cacheMissCounter = Metrics.DefaultFactory.CreateCounter(
            "cache_misses_total",
            "Total number of cache misses");
        
        _cacheErrorCounter = Metrics.DefaultFactory.CreateCounter(
            "cache_errors_total",
            "Total number of cache errors");
        
        // 业务操作相关指标
        _businessOperationCounter = Metrics.DefaultFactory.CreateCounter(
            "business_operations_total",
            "Total number of business operations");
        
        _businessErrorCounter = Metrics.DefaultFactory.CreateCounter(
            "business_errors_total",
            "Total number of business errors");
        
        _businessOperationDurationHistogram = Metrics.DefaultFactory.CreateHistogram(
            "business_operation_duration_seconds",
            "Duration of business operations in seconds",
            buckets: new[] { 0.001, 0.01, 0.1, 0.5, 1, 2, 5, 10 });
    }
    
    /// <summary>
    /// 记录API调用次数
    /// </summary>
    /// <param name="endpoint">API端点</param>
    /// <param name="method">HTTP方法</param>
    public void RecordApiCall(string endpoint, string method)
    {
        _apiCallCounter.Inc();
    }
    
    /// <summary>
    /// 记录数据库查询次数
    /// </summary>
    /// <param name="tableName">表名</param>
    public void RecordDatabaseQuery(string tableName)
    {
        _databaseQueryCounter.Inc();
    }
    
    /// <summary>
    /// 记录外部API调用次数
    /// </summary>
    /// <param name="apiName">API名称</param>
    public void RecordExternalApiCall(string apiName)
    {
        _externalApiCallCounter.Inc();
    }
    
    /// <summary>
    /// 记录缓存命中次数
    /// </summary>
    /// <param name="cacheName">缓存名称</param>
    public void RecordCacheHit(string cacheName)
    {
        _cacheHitCounter.Inc();
    }
    
    /// <summary>
    /// 记录缓存未命中次数
    /// </summary>
    /// <param name="cacheName">缓存名称</param>
    public void RecordCacheMiss(string cacheName)
    {
        _cacheMissCounter.Inc();
    }
    
    /// <summary>
    /// 记录业务处理时间
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <param name="durationMs">持续时间（毫秒）</param>
    public void RecordOperationDuration(string operationName, double durationMs)
    {
        // 将毫秒转换为秒
        var durationSeconds = durationMs / 1000;
        _businessOperationDurationHistogram.Observe(durationSeconds);
    }
    
    /// <summary>
    /// 记录API错误
    /// </summary>
    public void RecordApiError()
    {
        _apiErrorCounter.Inc();
    }
    
    /// <summary>
    /// 记录API请求持续时间
    /// </summary>
    /// <param name="durationMs">持续时间（毫秒）</param>
    public void RecordApiRequestDuration(double durationMs)
    {
        var durationSeconds = durationMs / 1000;
        _apiRequestDurationHistogram.Observe(durationSeconds);
    }
    
    /// <summary>
    /// 记录数据库错误
    /// </summary>
    public void RecordDatabaseError()
    {
        _databaseErrorCounter.Inc();
    }
    
    /// <summary>
    /// 记录数据库查询持续时间
    /// </summary>
    /// <param name="durationMs">持续时间（毫秒）</param>
    public void RecordDatabaseQueryDuration(double durationMs)
    {
        var durationSeconds = durationMs / 1000;
        _databaseQueryDurationHistogram.Observe(durationSeconds);
    }
    
    /// <summary>
    /// 记录外部API错误
    /// </summary>
    public void RecordExternalApiError()
    {
        _externalApiErrorCounter.Inc();
    }
    
    /// <summary>
    /// 记录外部API持续时间
    /// </summary>
    /// <param name="durationMs">持续时间（毫秒）</param>
    public void RecordExternalApiDuration(double durationMs)
    {
        var durationSeconds = durationMs / 1000;
        _externalApiDurationHistogram.Observe(durationSeconds);
    }
    
    /// <summary>
    /// 记录缓存错误
    /// </summary>
    public void RecordCacheError()
    {
        _cacheErrorCounter.Inc();
    }
    
    /// <summary>
    /// 记录业务操作
    /// </summary>
    public void RecordBusinessOperation()
    {
        _businessOperationCounter.Inc();
    }
    
    /// <summary>
    /// 记录业务错误
    /// </summary>
    public void RecordBusinessError()
    {
        _businessErrorCounter.Inc();
    }
}
