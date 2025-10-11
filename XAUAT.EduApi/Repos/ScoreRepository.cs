using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class ScoreRepository(EduContext context) : RepositoryBase<ScoreResponse>(context), IScoreRepository
{
    public async Task<IEnumerable<ScoreResponse>> GetByUserIdAsync(string userId)
    {
        return await Context.Scores
            .Where(s => EF.Property<string>(s, "UserId") == userId)
            .ToListAsync();
    }

    public async Task<ScoreResponse?> GetByUserAndLessonAsync(string userId, string lessonCode)
    {
        return await Context.Scores
            .FirstOrDefaultAsync(s => EF.Property<string>(s, "UserId") == userId && s.LessonCode == lessonCode);
    }

    public async Task AddRangeAsync(IEnumerable<ScoreResponse> scores)
    {
        await Context.Scores.AddRangeAsync(scores);
        await Context.SaveChangesAsync();
    }
}