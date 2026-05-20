namespace XAUAT.EduApi.Extensions;

public static class RequestExtensions
{
    public static string GetEduAuthCookie(this HttpRequest request)
    {
        var cookie = request.Headers.Cookie.ToString();
        if (!string.IsNullOrEmpty(cookie) && !cookie.StartsWith("Rider") && cookie.Contains("__pstsid__"))
        {
            return cookie;
        }

        return request.Headers["xauat"].ToString();
    }
}
