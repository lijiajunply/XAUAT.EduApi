namespace XAUAT.EduApi.Localization;

public interface IApiMessageLocalizer
{
    string Get(string? language, string key);
}
