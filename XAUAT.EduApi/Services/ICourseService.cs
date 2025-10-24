using EduApi.Data.Models;

namespace XAUAT.EduApi.Services;

public interface ICourseService
{
    Task<List<CourseActivity>> GetCoursesAsync(string studentId, string cookie);
}