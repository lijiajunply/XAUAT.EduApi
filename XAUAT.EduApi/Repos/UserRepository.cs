using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class UserRepository(EduContext context) : RepositoryBase<UserModel>(context), IUserRepository
{
    public async Task<UserModel?> GetByUsernameAsync(string username)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task UpdateScoreResponsesUpdateTimeAsync(string userId, string updateTime)
    {
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.ScoreResponsesUpdateTime = updateTime;
            await Context.SaveChangesAsync();
        }
    }
}