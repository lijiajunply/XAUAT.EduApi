namespace XAUAT.EduApi.Events;

/// <summary>
/// 事件总线，负责事件的发布和订阅管理
/// </summary>
public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _handlers = [];
    private readonly object _lock = new();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="logger">日志记录器</param>
    public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>任务</returns>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        
        _logger.LogInformation("Publishing event: {EventType} (ID: {EventId})", @event.EventType, @event.Id);
        
        var eventType = typeof(TEvent);
        
        // 直接调用已注册的处理程序
        List<Func<IEvent, CancellationToken, Task>>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
            {
                _logger.LogWarning("No handlers found for event: {EventType}", @event.EventType);
                return;
            }
            
            // 创建处理程序列表的副本，避免在处理过程中修改
            handlers = new List<Func<IEvent, CancellationToken, Task>>(handlers);
        }
        
        // 使用依赖注入创建的处理程序
        await using var scope = _serviceProvider.CreateAsyncScope();
        var scopeProvider = scope.ServiceProvider;
        var eventHandlers = scopeProvider.GetServices<IEventHandler<TEvent>>();
        
        // 合并所有处理程序
        var allHandlers = handlers.Select(handler => handler(@event, cancellationToken))
            .Concat(eventHandlers.Select(handler => handler.HandleAsync(@event, cancellationToken)));
        
        // 并行执行所有处理程序
        await Task.WhenAll(allHandlers);
        
        _logger.LogInformation("Event published successfully: {EventType} (ID: {EventId})", @event.EventType, @event.Id);
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="handler">事件处理程序</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var eventType = typeof(TEvent);
        
        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = [];
            }
            
            _handlers[eventType].Add((e, ct) => handler((TEvent)e, ct));
        }
        
        _logger.LogInformation("Subscribed to event: {EventType}", eventType.Name);
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理程序类型</typeparam>
    public void Subscribe<TEvent, THandler>() where TEvent : IEvent where THandler : IEventHandler<TEvent>
    {
        // 这种订阅方式依赖于依赖注入容器，在发布事件时通过服务提供程序获取处理程序
        _logger.LogInformation("Subscribed to event: {EventType} with handler: {HandlerType}", typeof(TEvent).Name, typeof(THandler).Name);
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="handler">事件处理程序</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var eventType = typeof(TEvent);
        
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                // 移除匹配的处理程序
                var toRemove = handlers.FirstOrDefault(h => h.Method.Equals(handler.Method));
                if (toRemove != null)
                {
                    handlers.Remove(toRemove);
                    _logger.LogInformation("Unsubscribed from event: {EventType}", eventType.Name);
                }
            }
        }
    }
    
    /// <summary>
    /// 取消订阅所有事件处理程序
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void UnsubscribeAll<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        
        lock (_lock)
        {
            if (_handlers.Remove(eventType))
            {
                _logger.LogInformation("Unsubscribed all handlers from event: {EventType}", eventType.Name);
            }
        }
    }
    
    /// <summary>
    /// 获取指定事件类型的处理程序数量
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>处理程序数量</returns>
    public int GetHandlerCount<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        
        lock (_lock)
        {
            return _handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }
}
