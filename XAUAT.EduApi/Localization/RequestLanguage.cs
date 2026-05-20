namespace XAUAT.EduApi.Localization;

public static class RequestLanguage
{
    public const string HeaderName = "x-language";
    public const string Chinese = "zh";
    public const string English = "en";

    public static string Normalize(string? language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            English => English,
            _ => Chinese
        };
    }
}
