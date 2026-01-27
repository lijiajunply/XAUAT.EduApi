namespace XAUAT.EduApi.Interfaces;

public interface ICookieCodeService
{
    Task<string> GetCode(string cookies);
    string ParseCookie(IEnumerable<string> cookies);
}
