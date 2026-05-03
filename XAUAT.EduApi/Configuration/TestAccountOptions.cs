namespace XAUAT.EduApi.Configuration;

/// <summary>
/// 测试账号配置
/// </summary>
public class TestAccountOptions
{
    public const string SectionName = "TestAccount";

    public bool Enabled { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string CookieMarker { get; set; } = "";
    public string FixturePath { get; set; } = "TestFixtures";
}
