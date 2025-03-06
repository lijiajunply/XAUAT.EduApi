namespace XAUAT.EduApi.Services;

public interface ILoginService
{
    Task<object> LoginAsync(string username, string password);
}