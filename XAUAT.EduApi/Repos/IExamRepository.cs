using EduApi.Data.Models;

namespace XAUAT.EduApi.Repos;

public interface IExamRepository : IRepository<ExamRecord>
{
    Task<IEnumerable<ExamRecord>> GetByStudentIdAsync(string studentId);
    Task MergeStudentExamsAsync(string studentId, IEnumerable<ExamRecord> newExams);
    Task<int> DeleteExpiredAsync(DateTime cutoff);
}
