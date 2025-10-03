using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class UserRepository : RepositoryBase<UserModel>, IUserRepository
{
    public UserRepository(EduContext context) : base(context)
    {
    }

    public async Task<UserModel?> GetByUsernameAsync(string username)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task AddSemesterAsync(string userId, string semester)
    {
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.Semesters.Add(semester);
            user.SemesterUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await Context.SaveChangesAsync();
        }
    }

    public async Task UpdateSemestersAsync(string userId, List<string> semesters)
    {
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.Semesters = semesters;
            user.SemesterUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await Context.SaveChangesAsync();
        }
    }

    public async Task UpdateSemesterUpdateTimeAsync(string userId, string updateTime)
    {
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.SemesterUpdateTime = updateTime;
            await Context.SaveChangesAsync();
        }
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