namespace XAUAT.EduApi.Extensions;

/// <summary>
/// HTTP客户端扩展方法
/// 用于统一配置HTTP请求头
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// 默认的现代浏览器User-Agent
    /// </summary>
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0";

    /// <param name="client">HTTP客户端</param>
    extension(HttpClient client)
    {
        /// <summary>
        /// 配置教务系统请求所需的标准请求头
        /// </summary>
        /// <param name="cookie">Cookie值</param>
        /// <returns>配置后的HTTP客户端</returns>
        public HttpClient ConfigureForEduSystem(string cookie)
        {
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            return client;
        }

        /// <summary>
        /// 配置教务系统请求所需的标准请求头（带超时设置）
        /// </summary>
        /// <param name="cookie">Cookie值</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>配置后的HTTP客户端</returns>
        public HttpClient ConfigureForEduSystem(string cookie, TimeSpan timeout)
        {
            client.ConfigureForEduSystem(cookie);
            client.Timeout = timeout;
            return client;
        }
    }

    /// <param name="request">HTTP请求消息</param>
    extension(HttpRequestMessage request)
    {
        /// <summary>
        /// 为HttpRequestMessage添加Cookie头
        /// </summary>
        /// <param name="cookie">Cookie值</param>
        /// <returns>配置后的HTTP请求消息</returns>
        public HttpRequestMessage WithCookie(string cookie)
        {
            request.Headers.Add("Cookie", cookie);
            return request;
        }

        /// <summary>
        /// 为HttpRequestMessage添加自定义头
        /// </summary>
        /// <param name="headers">请求头字典</param>
        /// <returns>配置后的HTTP请求消息</returns>
        public HttpRequestMessage WithHeaders(IDictionary<string, string> headers)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
            return request;
        }
    }
}
