namespace XAUAT.EduApi.Localization;

public class HeaderLanguageResolver : ILanguageResolver
{
    public string Resolve(HttpContext? httpContext)
    {
        var language = httpContext?.Request.Headers[RequestLanguage.HeaderName].ToString();
        return RequestLanguage.Normalize(language);
    }
}
