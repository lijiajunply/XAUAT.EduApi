using System.Collections.Generic;
using System.Threading.Tasks;

namespace XAUAT.EduApi.Services
{
    public interface IRedisService
    {
        IAsyncEnumerable<object> ScanRedisKeys();
        Task<string> GetKeyValueAsync(string key);
    }
}