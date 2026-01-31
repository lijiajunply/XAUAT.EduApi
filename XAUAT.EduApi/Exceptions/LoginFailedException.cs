namespace XAUAT.EduApi.Exceptions;

/// <summary>
/// 登录失败异常类
/// 当用户名或密码错误时抛出
/// </summary>
public class LoginFailedException : Exception
{
    /// <summary>
    /// 初始化LoginFailedException类的新实例
    /// </summary>
    public LoginFailedException() : base("用户名或密码错误")
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化LoginFailedException类的新实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public LoginFailedException(string message) : base(message)
    {
    }
}
