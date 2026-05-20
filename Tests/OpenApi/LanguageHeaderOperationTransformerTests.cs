using XAUAT.EduApi.Localization;
using XAUAT.EduApi.OpenApi;

namespace XAUAT.EduApi.Tests.OpenApi;

public class LanguageHeaderOperationTransformerTests
{
    [Fact]
    public void DefaultLanguage_ShouldBeSimplifiedChinese()
    {
        Assert.Equal(RequestLanguage.SimplifiedChinese, LanguageHeaderOperationTransformer.DefaultLanguage);
    }

    [Fact]
    public void SupportedLanguages_ShouldMatchPublicContract()
    {
        Assert.Equal(
            [
                RequestLanguage.German,
                RequestLanguage.Russian,
                RequestLanguage.French,
                RequestLanguage.Japanese,
                RequestLanguage.Korean,
                RequestLanguage.English,
                RequestLanguage.SimplifiedChinese,
                RequestLanguage.TraditionalChinese
            ],
            RequestLanguage.SupportedLanguages);
    }

    [Fact]
    public void Description_ShouldMentionSupportedLanguagesAndLegacyAliases()
    {
        Assert.Contains("de, ru, fr, ja, ko, en, zh-CN, zh-TW", LanguageHeaderOperationTransformer.Description);
        Assert.Contains("zh -> zh-CN", LanguageHeaderOperationTransformer.Description);
        Assert.Contains("Defaults to zh-CN", LanguageHeaderOperationTransformer.Description);
    }
}
