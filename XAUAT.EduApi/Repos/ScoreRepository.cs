using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class ScoreRepository(IDbContextFactory<EduContext> contextFactory)
    : RepositoryBase<ScoreResponse>(contextFactory), IScoreRepository
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
        var scoreResponses = scores as ScoreResponse[] ?? scores.ToArray();
        foreach (var score in scoreResponses)
        {
            TruncateIfNeeded(score);
        }

        await using var context = CreateContext();
        await context.Scores.AddRangeAsync(scoreResponses);
        await context.SaveChangesAsync();
    }

    private static void TruncateIfNeeded(ScoreResponse score)
    {
        if (score.Name.Length > 256) score.Name = score.Name[..256];
        if (score.LessonName.Length > 256) score.LessonName = score.LessonName[..256];
        if (score.Grade.Length > 256) score.Grade = score.Grade[..256];
        if (score.GradeDetail.Length > 512) score.GradeDetail = score.GradeDetail[..512];
    }
}