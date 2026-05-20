using XAUAT.EduApi.Localization;

namespace XAUAT.EduApi.Tests.Localization;

public class ApiMessageLocalizerTests
{
    private readonly ApiMessageLocalizer _localizer = new();

    [Theory]
    [InlineData(RequestLanguage.German, "Authentifizierung fehlgeschlagen. Bitte melden Sie sich erneut an.")]
    [InlineData(RequestLanguage.Russian, "Ошибка аутентификации. Пожалуйста, войдите снова.")]
    [InlineData(RequestLanguage.French, "Échec de l'authentification. Veuillez vous reconnecter.")]
    [InlineData(RequestLanguage.Japanese, "認証に失敗しました。もう一度ログインしてください。")]
    [InlineData(RequestLanguage.Korean, "인증에 실패했습니다. 다시 로그인해 주세요.")]
    [InlineData(RequestLanguage.English, "Authentication failed. Please sign in again.")]
    [InlineData(RequestLanguage.SimplifiedChinese, "认证失败，请重新登录")]
    [InlineData(RequestLanguage.TraditionalChinese, "認證失敗，請重新登入")]
    public void Get_ShouldReturnLocalizedAuthenticationMessage(string language, string expected)
    {
        var result = _localizer.Get(language, ApiMessageKey.AuthenticationFailed);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RequestLanguage.French, " (données issues de l'ancienne plateforme)")]
    [InlineData(RequestLanguage.TraditionalChinese, " (呼叫的是舊平台資料)")]
    public void Get_ShouldReturnLocalizedBusLegacySuffix(string language, string expected)
    {
        var result = _localizer.Get(language, ApiMessageKey.BusOldPlatformSuffix);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Get_ShouldFallbackToSimplifiedChinese_ForUnsupportedLanguage()
    {
        var result = _localizer.Get("it", ApiMessageKey.ServiceUnavailable);

        Assert.Equal("服务暂时不可用", result);
    }

    [Theory]
    [InlineData(RequestLanguage.English, "Unknown error.")]
    [InlineData(RequestLanguage.SimplifiedChinese, "未知错误")]
    [InlineData(RequestLanguage.TraditionalChinese, "未知錯誤")]
    public void Get_ShouldReturnLocalizedUnknownError_WhenKeyDoesNotExist(string language, string expected)
    {
        var result = _localizer.Get(language, "MissingKey");

        Assert.Equal(expected, result);
    }
}
