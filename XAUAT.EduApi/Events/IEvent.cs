namespace XAUAT.EduApi.Events;

/// <summary>
/// 事件接口，所有事件必须实现此接口
/// </summary>
public interface IEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// 事件发生时间
    /// </summary>
    DateTime OccurredAt { get; }
    
    /// <summary>
    /// 事件类型
    /// </summary>
    string EventType { get; }
}
