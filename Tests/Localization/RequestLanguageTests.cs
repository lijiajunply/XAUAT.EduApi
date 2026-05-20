using XAUAT.EduApi.Localization;

namespace XAUAT.EduApi.Tests.Localization;

public class RequestLanguageTests
{
    [Theory]
    [InlineData("de", RequestLanguage.German)]
    [InlineData("ru", RequestLanguage.Russian)]
    [InlineData("fr", RequestLanguage.French)]
    [InlineData("ja", RequestLanguage.Japanese)]
    [InlineData("ko", RequestLanguage.Korean)]
    [InlineData("en", RequestLanguage.English)]
    [InlineData("zh-CN", RequestLanguage.SimplifiedChinese)]
    [InlineData("zh-TW", RequestLanguage.TraditionalChinese)]
    [InlineData("ZH-cn", RequestLanguage.SimplifiedChinese)]
    [InlineData(" En ", RequestLanguage.English)]
    [InlineData("zh", RequestLanguage.SimplifiedChinese)]
    [InlineData("zh-Hans", RequestLanguage.SimplifiedChinese)]
    [InlineData("zh-Hant", RequestLanguage.TraditionalChinese)]
    [InlineData("it", RequestLanguage.SimplifiedChinese)]
    [InlineData("", RequestLanguage.SimplifiedChinese)]
    public void Normalize_ShouldReturnExpectedLanguage(string? input, string expected)
    {
        var result = RequestLanguage.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_ShouldFallbackToSimplifiedChinese_WhenLanguageIsNull()
    {
        var result = RequestLanguage.Normalize(null);

        Assert.Equal(RequestLanguage.SimplifiedChinese, result);
    }
}
