using XAUAT.EduApi.DataModels;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Services;

public interface IExamService
{
    Task<ExamResponse> GetExamArrangementsAsync(string cookie,string? id);

    Task<SemesterItem> GetThisSemester(string cookie);
}