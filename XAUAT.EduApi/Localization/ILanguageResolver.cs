namespace XAUAT.EduApi.Localization;

public interface ILanguageResolver
{
    string Resolve(HttpContext? httpContext);
}
