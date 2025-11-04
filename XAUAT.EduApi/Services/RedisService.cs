using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace XAUAT.EduApi.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _muxer;
        private readonly IDatabase _redis;

        public RedisService(IConnectionMultiplexer muxer)
        {
            _muxer = muxer;
            _redis = muxer.GetDatabase();
        }

        public async IAsyncEnumerable<object> ScanRedisKeys()
        {
            const string pattern = "*";
            const int database = 0;
            const int pageSize = 1000;
            const int batchSize = 500;
            const int maxKeys = 10000;
            var cancellationToken = CancellationToken.None;

            var endpoints = _muxer.GetEndPoints();

            var yielded = 0;

            foreach (var endpoint in endpoints)
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                var server = _muxer.GetServer(endpoint);

                // 跳过不可用/副本节点
                if (!server.IsConnected) continue;

                // Keys 返回 IEnumerable<RedisKey>，底层使用 SCAN，支持 pageSize 控制
                var keysEnumerable = server.Keys(database: database, pattern: pattern, pageSize: pageSize);

                // 分批读取 keys，然后用 MGET (StringGet) 获取值
                await foreach (var chunk in ChunkAsync(keysEnumerable, batchSize, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) yield break;
                    if (chunk.Length == 0) continue;

                    // 使用 StringGet 批量获取（仅适用于 string/byte[]）
                    var values = await _redis.StringGetAsync(chunk).ConfigureAwait(false);

                    for (var i = 0; i < chunk.Length; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) yield break;

                        var key = chunk[i].ToString();
                        var val = values.Length > i ? values[i] : RedisValue.Null;

                        var s = val.ToString();
                        // 返回简单对象（可按需扩展：增加 Type, TTL 等）
                        yield return new
                        {
                            Key = key,
                            Value = val.IsNull ? null : s
                        };

                        yielded++;
                        if (yielded >= maxKeys)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        public async Task<string> GetKeyValueAsync(string key)
        {
            var value = await _redis.StringGetAsync(key);
            return value.ToString();
        }

        // 将 IEnumerable<RedisKey> 分块并以异步迭代方式返回 RedisKey[]，避免一次性加载所有 keys 到内存
        private static async IAsyncEnumerable<RedisKey[]> ChunkAsync(IEnumerable<RedisKey> source, int chunkSize,
            [EnumeratorCancellation] CancellationToken ct)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
            var bucket = new List<RedisKey>(chunkSize);

            foreach (var item in source)
            {
                if (ct.IsCancellationRequested) yield break;
                bucket.Add(item);
                if (bucket.Count != chunkSize) continue;
                yield return bucket.ToArray();
                bucket.Clear();
                // 给调用方机会响应取消/IO
                await Task.Yield();
            }

            if (bucket.Count > 0)
            {
                yield return bucket.ToArray();
            }
        }
    }
}