using EduApi.Data.Models;

namespace XAUAT.EduApi.Repos;

public interface IUserRepository : IRepository<UserModel>
{
    Task<UserModel?> GetByUsernameAsync(string username);
    Task UpdateScoreResponsesUpdateTimeAsync(string userId, string updateTime);
}