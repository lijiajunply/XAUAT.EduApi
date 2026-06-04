using Serilog.Events;
using XAUAT.EduApi.Configuration;

namespace XAUAT.EduApi.Extensions;

public static class EnvironmentVariableHelper
{
    public static string? GetString(params string[] variableNames)
    {
        return variableNames
            .Select(variableName => Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    public static string GetStringOrDefault(string defaultValue, params string[] variableNames)
    {
        return GetString(variableNames) ?? defaultValue;
    }

    public static int GetIntOrDefault(int defaultValue, params string[] variableNames)
    {
        var value = GetString(variableNames);
        return int.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }

    public static bool GetBoolOrDefault(bool defaultValue, params string[] variableNames)
    {
        var value = GetString(variableNames);
        return bool.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }

    public static LogEventLevel GetLogEventLevelOrDefault(LogEventLevel defaultValue, params string[] variableNames)
    {
        var value = GetString(variableNames);
        return Enum.TryParse<LogEventLevel>(value, true, out var parsedValue) ? parsedValue : defaultValue;
    }

    public static ElectricitySubscriptionOptions BuildElectricitySubscriptionOptions()
    {
        return new ElectricitySubscriptionOptions
        {
            ScanIntervalMinutes = GetIntOrDefault(
                15,
                "ELECTRICITY_SUBSCRIPTION_SCAN_INTERVAL_MINUTES",
                "ElectricitySubscription__ScanIntervalMinutes"),
            NotificationCooldownMinutes = GetIntOrDefault(
                720,
                "ELECTRICITY_SUBSCRIPTION_NOTIFICATION_COOLDOWN_MINUTES",
                "ElectricitySubscription__NotificationCooldownMinutes"),
            BatchSize = GetIntOrDefault(
                50,
                "ELECTRICITY_SUBSCRIPTION_BATCH_SIZE",
                "ElectricitySubscription__BatchSize")
        };
    }

    public static SmtpOptions BuildSmtpOptions()
    {
        return new SmtpOptions
        {
            Host = GetStringOrDefault("", "SMTP_HOST", "Smtp__Host"),
            Port = GetIntOrDefault(587, "SMTP_PORT", "Smtp__Port"),
            UserName = GetStringOrDefault("", "SMTP_USERNAME", "Smtp__UserName"),
            Password = GetStringOrDefault("", "SMTP_PASSWORD", "Smtp__Password"),
            EnableSsl = GetBoolOrDefault(true, "SMTP_ENABLE_SSL", "Smtp__EnableSsl"),
            FromAddress = GetStringOrDefault("", "SMTP_FROM_ADDRESS", "Smtp__FromAddress"),
            FromName = GetStringOrDefault("XAUAT EduApi", "SMTP_FROM_NAME", "Smtp__FromName")
        };
    }

    public static TestAccountOptions BuildTestAccountOptions()
    {
        return new TestAccountOptions
        {
            Enabled = GetBoolOrDefault(false, "TEST_ACCOUNT_ENABLED", "TestAccount__Enabled"),
            Username = GetStringOrDefault("frontend-test", "TEST_ACCOUNT_USERNAME", "TestAccount__Username"),
            Password = GetStringOrDefault("frontend-test-password", "TEST_ACCOUNT_PASSWORD", "TestAccount__Password"),
            StudentId = GetStringOrDefault("20239999", "TEST_ACCOUNT_STUDENT_ID", "TestAccount__StudentId"),
            CookieMarker = GetStringOrDefault("frontend-test-marker", "TEST_ACCOUNT_COOKIE_MARKER",
                "TestAccount__CookieMarker"),
            FixturePath = GetStringOrDefault("TestFixtures", "TEST_ACCOUNT_FIXTURE_PATH", "TestAccount__FixturePath")
        };
    }

    public static MapAdminOptions BuildMapAdminOptions()
    {
        return new MapAdminOptions
        {
            Token = GetStringOrDefault("", "MAP_ADMIN_TOKEN", "MapAdmin__Token")
        };
    }
}
