using EduApi.Data.Models;

namespace XAUAT.EduApi.Repos;

public interface IUserRepository : IRepository<UserModel>
{
    Task<UserModel?> GetByUsernameAsync(string username);
    Task AddSemesterAsync(string userId, string semester);
    Task UpdateSemestersAsync(string userId, List<string> semesters);
    Task UpdateSemesterUpdateTimeAsync(string userId, string updateTime);
    Task UpdateScoreResponsesUpdateTimeAsync(string userId, string updateTime);
}