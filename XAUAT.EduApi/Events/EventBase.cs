namespace XAUAT.EduApi.Events;

/// <summary>
/// 事件基类，所有具体事件应继承此类
/// </summary>
public abstract class EventBase : IEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    public Guid Id { get; }
    
    /// <summary>
    /// 事件发生时间
    /// </summary>
    public DateTime OccurredAt { get; }
    
    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    protected EventBase()
    {
        Id = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        EventType = GetType().Name;
    }
}
