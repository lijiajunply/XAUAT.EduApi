using XAUAT.EduApi.Configuration;

namespace XAUAT.EduApi.Extensions;

/// <summary>
/// 服务配置类
/// 用于封装所有服务配置信息
/// </summary>
public class ServiceConfiguration
{
    /// <summary>
    /// SQL连接字符串
    /// </summary>
    public string? SqlConnectionString { get; set; }

    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// 是否启用Prometheus监控
    /// </summary>
    public bool EnablePrometheus { get; set; } = true;

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 测试账号配置
    /// </summary>
    public TestAccountOptions TestAccount { get; set; } = new();

    /// <summary>
    /// 从配置中创建ServiceConfiguration实例
    /// </summary>
    /// <param name="configuration">配置</param>
    /// <returns>ServiceConfiguration实例</returns>
    public static ServiceConfiguration FromConfiguration(IConfiguration configuration)
    {
        var testAccount = new TestAccountOptions();
        configuration.GetSection(TestAccountOptions.SectionName).Bind(testAccount);

        if (bool.TryParse(Environment.GetEnvironmentVariable("TEST_ACCOUNT_ENABLED", EnvironmentVariableTarget.Process), out var enabled))
        {
            testAccount.Enabled = enabled;
        }

        testAccount.Username = Environment.GetEnvironmentVariable("TEST_ACCOUNT_USERNAME", EnvironmentVariableTarget.Process) ??
                               testAccount.Username;
        testAccount.Password = Environment.GetEnvironmentVariable("TEST_ACCOUNT_PASSWORD", EnvironmentVariableTarget.Process) ??
                               testAccount.Password;
        testAccount.StudentId = Environment.GetEnvironmentVariable("TEST_ACCOUNT_STUDENT_ID", EnvironmentVariableTarget.Process) ??
                                testAccount.StudentId;
        testAccount.CookieMarker = Environment.GetEnvironmentVariable("TEST_ACCOUNT_COOKIE_MARKER", EnvironmentVariableTarget.Process) ??
                                   testAccount.CookieMarker;
        testAccount.FixturePath = Environment.GetEnvironmentVariable("TEST_ACCOUNT_FIXTURE_PATH", EnvironmentVariableTarget.Process) ??
                                  testAccount.FixturePath;

        return new ServiceConfiguration
        {
            SqlConnectionString = Environment.GetEnvironmentVariable("SQL", EnvironmentVariableTarget.Process),
            RedisConnectionString = Environment.GetEnvironmentVariable("REDIS", EnvironmentVariableTarget.Process) ??
                                    configuration["Redis"],
            EnablePrometheus = configuration.GetValue<bool>("Prometheus:Enabled", true),
            EnableLogging = configuration.GetValue<bool>("Logging:Enabled", true),
            TestAccount = testAccount
        };
    }
}
