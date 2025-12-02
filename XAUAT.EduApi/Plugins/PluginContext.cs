namespace XAUAT.EduApi.Plugins;

/// <summary>
/// 插件上下文，提供插件访问应用程序服务和配置的能力
/// </summary>
public class PluginContext
{
    /// <summary>
    /// 服务集合
    /// </summary>
    public IServiceCollection Services { get; }
    
    /// <summary>
    /// 配置对象
    /// </summary>
    public IConfiguration Configuration { get; }
    
    /// <summary>
    /// 日志工厂
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="loggerFactory">日志工厂</param>
    public PluginContext(IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        Services = services;
        Configuration = configuration;
        LoggerFactory = loggerFactory;
    }
}
