namespace XAUAT.EduApi.Controllers.V1;

public static class ApiCodes
{
    /// <summary>成功</summary>
    public const int Success = 0;

    /// <summary>参数缺失或格式错误</summary>
    public const int ParamError = 10001;

    /// <summary>认证失败（Cookie过期或无效）</summary>
    public const int AuthFailed = 20001;

    /// <summary>用户名或密码错误</summary>
    public const int WrongCredentials = 20002;

    /// <summary>资源未找到</summary>
    public const int NotFound = 30001;

    /// <summary>请求频率限制</summary>
    public const int RateLimited = 40001;

    /// <summary>服务器内部错误</summary>
    public const int InternalError = 50001;

    /// <summary>上游服务访问失败</summary>
    public const int UpstreamError = 50002;

    /// <summary>数据获取失败</summary>
    public const int DataFetchFailed = 50003;
}
