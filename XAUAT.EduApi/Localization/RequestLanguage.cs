namespace XAUAT.EduApi.Localization;

public static class RequestLanguage
{
    public const string HeaderName = "x-language";
    public const string German = "de";
    public const string Russian = "ru";
    public const string French = "fr";
    public const string Japanese = "ja";
    public const string Korean = "ko";
    public const string English = "en";
    public const string SimplifiedChinese = "zh";
    public const string TraditionalChinese = "zh-Hant";

    public static readonly string[] SupportedLanguages =
    [
        German,
        Russian,
        French,
        Japanese,
        Korean,
        English,
        SimplifiedChinese,
        TraditionalChinese
    ];

    public static string Normalize(string? language)
    {
        var normalized = language?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return SimplifiedChinese;
        }

        return normalized.ToLowerInvariant() switch
        {
            German => German,
            Russian => Russian,
            French => French,
            Japanese => Japanese,
            Korean => Korean,
            English => English,
            "zh-cn" or "zh-hans" => SimplifiedChinese,
            "zh-tw" or "zh-hant" => TraditionalChinese,
            _ => SimplifiedChinese
        };
    }
}