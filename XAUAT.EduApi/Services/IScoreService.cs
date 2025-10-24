using EduApi.Data.Models;

namespace XAUAT.EduApi.Services;

public interface IScoreService
{
    Task<List<ScoreResponse>> GetScoresAsync(string studentId, string semester, string cookie);
    Task<SemesterResult> ParseSemesterAsync(string? studentId, string cookie);
    Task<SemesterItem> GetThisSemesterAsync(string cookie);
}