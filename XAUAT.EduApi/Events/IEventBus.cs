namespace XAUAT.EduApi.Events;

/// <summary>
/// 事件总线接口，定义事件总线的基本操作
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>任务</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="handler">事件处理程序</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理程序类型</typeparam>
    void Subscribe<TEvent, THandler>() where TEvent : IEvent where THandler : IEventHandler<TEvent>;
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="handler">事件处理程序</param>
    /// <typeparam name="TEvent">事件类型</typeparam>
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IEvent;
    
    /// <summary>
    /// 取消订阅所有事件处理程序
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    void UnsubscribeAll<TEvent>() where TEvent : IEvent;
    
    /// <summary>
    /// 获取指定事件类型的处理程序数量
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <returns>处理程序数量</returns>
    int GetHandlerCount<TEvent>() where TEvent : IEvent;
}
