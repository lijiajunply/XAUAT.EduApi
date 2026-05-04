namespace XAUAT.EduApi.Configuration;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = "";

    public string Password { get; set; } = "";

    public bool EnableSsl { get; set; } = true;

    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "XAUAT EduApi";
}
