using System.Reflection;

namespace XAUAT.EduApi.Plugins;

/// <summary>
/// 插件管理器，负责管理插件的加载、初始化、启动、停止和卸载
/// </summary>
public class PluginManager : IDisposable
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly List<IPlugin> _plugins = [];
    private readonly string _pluginsDirectory;
    private bool _isInitialized = false;
    
    /// <summary>
    /// 已加载的插件列表
    /// </summary>
    public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="loggerFactory">日志工厂</param>
    public PluginManager(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PluginManager>();
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _pluginsDirectory = configuration.GetValue<string>("Plugins:Directory") ?? "./Plugins";
    }
    
    /// <summary>
    /// 初始化插件管理器
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        
        _logger.LogInformation("Initializing plugin manager");
        
        // 确保插件目录存在
        Directory.CreateDirectory(_pluginsDirectory);
        
        _isInitialized = true;
    }
    
    /// <summary>
    /// 加载所有插件
    /// </summary>
    public void LoadPlugins()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        _logger.LogInformation("Loading plugins from directory: {Directory}", _pluginsDirectory);
        
        try
        {
            // 首先加载内置插件
            LoadBuiltInPlugins();
            
            // 然后加载外部插件
            LoadExternalPlugins();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugins");
        }
    }
    
    /// <summary>
    /// 加载内置插件（同一程序集中的插件）
    /// </summary>
    private void LoadBuiltInPlugins()
    {
        _logger.LogInformation("Loading built-in plugins");
        
        var assembly = Assembly.GetExecutingAssembly();
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
        
        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                LoadPlugin(plugin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load built-in plugin: {PluginType}", pluginType.FullName);
            }
        }
    }
    
    /// <summary>
    /// 加载外部插件（从DLL文件加载）
    /// </summary>
    private void LoadExternalPlugins()
    {
        _logger.LogInformation("Loading external plugins");
        
        // 获取插件目录中的所有DLL文件
        var dllFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
        
        foreach (var dllFile in dllFiles)
        {
            try
            {
                // 加载程序集
                var assembly = Assembly.LoadFrom(dllFile);
                
                // 查找所有实现了IPlugin接口的类型
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
                
                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                        LoadPlugin(plugin);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to instantiate plugin: {PluginType}", pluginType.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin assembly: {DllFile}", dllFile);
            }
        }
    }
    
    /// <summary>
    /// 加载单个插件
    /// </summary>
    /// <param name="plugin">插件实例</param>
    private void LoadPlugin(IPlugin plugin)
    {
        if (_plugins.Any(p => p.Name == plugin.Name))
        {
            _logger.LogWarning("Plugin {PluginName} is already loaded, skipping", plugin.Name);
            return;
        }
        
        _logger.LogInformation("Loading plugin: {PluginName} v{Version}", plugin.Name, plugin.Version);
        
        try
        {
            // 创建插件上下文
            var services = new ServiceCollection();
            // 将现有服务提供程序的服务添加到新的服务集合中
            // 注意：这是一个简化实现，实际应用中可能需要更复杂的服务转换
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var pluginContext = new PluginContext(services, _configuration, loggerFactory);
            
            // 初始化插件
            plugin.Initialize(pluginContext);
            
            // 启动插件
            plugin.Start();
            
            // 添加到插件列表
            _plugins.Add(plugin);
            
            _logger.LogInformation("Plugin {PluginName} loaded successfully", plugin.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize or start plugin: {PluginName}", plugin.Name);
        }
    }
    
    /// <summary>
    /// 启动所有插件
    /// </summary>
    public void StartAllPlugins()
    {
        _logger.LogInformation("Starting all plugins");
        
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Start();
                _logger.LogInformation("Plugin {PluginName} started successfully", plugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start plugin: {PluginName}", plugin.Name);
            }
        }
    }
    
    /// <summary>
    /// 停止所有插件
    /// </summary>
    public void StopAllPlugins()
    {
        _logger.LogInformation("Stopping all plugins");
        
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Stop();
                _logger.LogInformation("Plugin {PluginName} stopped successfully", plugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop plugin: {PluginName}", plugin.Name);
            }
        }
    }
    
    /// <summary>
    /// 卸载所有插件
    /// </summary>
    public void UnloadAllPlugins()
    {
        _logger.LogInformation("Unloading all plugins");
        
        foreach (var plugin in _plugins.ToList())
        {
            try
            {
                plugin.Unload();
                _plugins.Remove(plugin);
                _logger.LogInformation("Plugin {PluginName} unloaded successfully", plugin.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin: {PluginName}", plugin.Name);
            }
        }
    }
    
    /// <summary>
    /// 根据名称查找插件
    /// </summary>
    /// <param name="name">插件名称</param>
    /// <returns>插件实例，如果未找到则返回null</returns>
    public IPlugin? FindPlugin(string name)
    {
        return _plugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        UnloadAllPlugins();
    }
}
