using EduApi.Data.Models;

namespace XAUAT.EduApi.Repos;

public interface IScoreRepository : IRepository<ScoreResponse>
{
    Task<IEnumerable<ScoreResponse>> GetByUserIdAsync(string userId);
    Task<ScoreResponse?> GetByUserAndLessonAsync(string userId, string lessonCode);
    Task AddRangeAsync(IEnumerable<ScoreResponse> scores);
}