namespace XAUAT.EduApi.Services;

public interface IProgramService
{
    public Task<List<PlanCourse>> GetAllTrainProgram(string cookie, string id);
    
    public Task<Dictionary<string, List<PlanCourse>>> GetAllTrainPrograms(string cookie, string id);
}