using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Repos;

public class ExamRepository(IDbContextFactory<EduContext> contextFactory)
    : RepositoryBase<ExamRecord>(contextFactory), IExamRepository
{
    public async Task<IEnumerable<ExamRecord>> GetByStudentIdAsync(string studentId)
    {
        await using var context = CreateContext();
        return await context.ExamRecords.AsNoTracking()
            .Where(e => EF.Property<string>(e, "StudentId") == studentId)
            .ToListAsync();
    }

    public async Task MergeStudentExamsAsync(string studentId, IEnumerable<ExamRecord> newExams)
    {
        var newList = newExams as ExamRecord[] ?? newExams.ToArray();
        foreach (var exam in newList)
        {
            TruncateIfNeeded(exam);
        }

        await using var context = CreateContext();
        var oldRecords = await context.ExamRecords
            .Where(e => e.StudentId == studentId)
            .ToListAsync();

        var oldKeySet = new HashSet<string>(oldRecords.Select(e => e.Key));
        var newKeySet = new HashSet<string>(newList.Select(e => e.Key));

        var toDelete = oldRecords.Where(e => !newKeySet.Contains(e.Key)).ToList();
        var toAdd = newList.Where(e => !oldKeySet.Contains(e.Key)).ToList();

        var oldDict = oldRecords.ToDictionary(e => e.Key);
        foreach (var newExam in newList.Where(e => oldKeySet.Contains(e.Key)))
        {
            var oldExam = oldDict[newExam.Key];
            if (oldExam.Name == newExam.Name &&
                oldExam.Time == newExam.Time &&
                oldExam.ExamTime == newExam.ExamTime &&
                oldExam.Location == newExam.Location &&
                oldExam.Seat == newExam.Seat) continue;
            newExam.StudentId = studentId;
            context.Entry(oldExam).CurrentValues.SetValues(newExam);
        }

        if (toDelete.Count > 0)
            context.ExamRecords.RemoveRange(toDelete);
        if (toAdd.Count > 0)
            await context.ExamRecords.AddRangeAsync(toAdd);

        await context.SaveChangesAsync();
    }

    public async Task<int> DeleteExpiredAsync(DateTime cutoff)
    {
        await using var context = CreateContext();
        var expired = await context.ExamRecords
            .Where(e => e.ExamTime < cutoff)
            .ToListAsync();

        if (expired.Count == 0)
            return 0;

        context.ExamRecords.RemoveRange(expired);
        return await context.SaveChangesAsync();
    }

    private static void TruncateIfNeeded(ExamRecord exam)
    {
        if (exam.Name.Length > 256) exam.Name = exam.Name[..256];
        if (exam.Time.Length > 256) exam.Time = exam.Time[..256];
        if (exam.Location.Length > 256) exam.Location = exam.Location[..256];
        if (exam.Seat.Length > 64) exam.Seat = exam.Seat[..64];
    }
}