using Microsoft.AspNetCore.Mvc;

namespace XAUAT.EduApi.Localization;

public abstract class LanguageAwareControllerBase(
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer) : ControllerBase
{
    protected string Language => languageResolver.Resolve(HttpContext);

    protected string Message(string key)
    {
        return messageLocalizer.Get(Language, key);
    }
}
