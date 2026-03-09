namespace XAUAT.EduApi.Extensions;

/// <summary>
/// HTTP超时配置
/// 统一管理所有HTTP请求的超时时间
/// </summary>
public static class HttpTimeouts
{
    /// <summary>
    /// 默认超时时间（用于一般请求）
    /// </summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 快速请求超时时间（用于简单查询）
    /// </summary>
    public static readonly TimeSpan Fast = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 慢速请求超时时间（用于复杂操作，如登录、支付）
    /// </summary>
    public static readonly TimeSpan Slow = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 教务系统请求超时时间
    /// </summary>
    public static readonly TimeSpan EduSystem = TimeSpan.FromSeconds(8);

    /// <summary>
    /// 外部API请求超时时间
    /// </summary>
    public static readonly TimeSpan ExternalApi = TimeSpan.FromSeconds(10);
}
