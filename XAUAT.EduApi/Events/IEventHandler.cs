namespace XAUAT.EduApi.Events;

/// <summary>
/// 事件处理程序接口，用于处理特定类型的事件
/// </summary>
/// <typeparam name="TEvent">事件类型</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// 处理事件
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
