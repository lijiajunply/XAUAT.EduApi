namespace XAUAT.EduApi.Exceptions;

/// <summary>
/// 未认证异常类
/// 当用户需要重新登录或Cookie失效时抛出
/// </summary>
public class UnAuthenticationError : Exception
{
    /// <summary>
    /// 初始化UnAuthenticationError类的新实例
    /// </summary>
    public UnAuthenticationError() : base("未认证，请重新登录")
    {
    }
}