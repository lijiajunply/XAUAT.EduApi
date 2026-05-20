namespace XAUAT.EduApi.Localization;

public class ApiMessageLocalizer : IApiMessageLocalizer
{
    private static readonly IReadOnlyDictionary<string, (string Zh, string En)> Messages =
        new Dictionary<string, (string Zh, string En)>
        {
            [ApiMessageKey.UsernameOrPasswordRequired] = ("用户名或密码不能为空", "Username and password are required."),
            [ApiMessageKey.InvalidUsernameOrPassword] = ("用户名或密码错误", "Invalid username or password."),
            [ApiMessageKey.EduSystemAccessFailed] = ("教务处系统访问失败，请联系管理员", "Failed to access the academic system. Please contact the administrator."),
            [ApiMessageKey.AuthenticationFailed] = ("认证失败，请重新登录", "Authentication failed. Please sign in again."),
            [ApiMessageKey.InternalServerError] = ("服务器内部错误", "Internal server error."),
            [ApiMessageKey.ExamFetchFailed] = ("获取考试安排失败", "Failed to get exam arrangements."),
            [ApiMessageKey.ProgramFetchFailed] = ("获取培养方案失败", "Failed to get training program."),
            [ApiMessageKey.CompletionFetchFailed] = ("获取学业进度失败", "Failed to get academic progress."),
            [ApiMessageKey.BusFetchFailed] = ("获取校车数据失败", "Failed to get bus schedule data."),
            [ApiMessageKey.BusOldPlatformSuffix] = (" (调用的为旧平台数据)", " (using legacy platform data)"),
            [ApiMessageKey.ServiceUnavailable] = ("服务暂时不可用", "Service temporarily unavailable."),
            [ApiMessageKey.PaymentLoginUnknownError] = ("登录过程中发生未知错误", "An unknown error occurred during login."),
            [ApiMessageKey.PaymentTurnoverUnknownError] = ("获取消费记录过程中发生未知错误", "An unknown error occurred while getting turnover records.")
        };

    public string Get(string? language, string key)
    {
        var normalizedLanguage = RequestLanguage.Normalize(language);
        if (!Messages.TryGetValue(key, out var message))
        {
            return normalizedLanguage == RequestLanguage.English
                ? ApiMessageKey.InternalServerError
                : "未知错误";
        }

        return normalizedLanguage == RequestLanguage.English ? message.En : message.Zh;
    }
}
