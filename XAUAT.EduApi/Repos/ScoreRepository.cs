using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class ScoreRepository(IDbContextFactory<EduContext> contextFactory) : RepositoryBase<ScoreResponse>(contextFactory), IScoreRepository
{
    public async Task<IEnumerable<ScoreResponse>> GetByUserIdAsync(string userId)
    {
        await using var context = CreateContext();
        return await context.Scores.AsNoTracking()
            .Where(s => EF.Property<string>(s, "UserId") == userId)
            .ToListAsync();
    }

    public async Task<ScoreResponse?> GetByUserAndLessonAsync(string userId, string lessonCode)
    {
        await using var context = CreateContext();
        return await context.Scores.AsNoTracking()
            .FirstOrDefaultAsync(s => EF.Property<string>(s, "UserId") == userId && s.LessonCode == lessonCode);
    }

    public async Task AddRangeAsync(IEnumerable<ScoreResponse> scores)
    {
        await using var context = CreateContext();
        await context.Scores.AddRangeAsync(scores);
        await context.SaveChangesAsync();
    }
}