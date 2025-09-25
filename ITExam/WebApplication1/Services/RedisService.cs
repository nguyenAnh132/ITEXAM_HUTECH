using StackExchange.Redis;

namespace ITExam.Services
{
    public interface IRedisService
    {
        Task AddLogAsync(string key, string log);
        Task<List<string>> GetLogsAsync(string key);
        Task DeleteLogsAsync(string key);
        Task DeleteKeyAsync(string key);
        Task<List<string>> SearchKeysAsync(string pattern);
        Task SetStringAsync(string key, string value);
        Task<string?> GetStringAsync(string key);
        Task DeleteKeysByPatternAsync(string pattern);
    }

    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;
        private static readonly TimeSpan LogTtl = TimeSpan.FromHours(2);

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        public async Task AddLogAsync(string key, string log)
        {
            await _db.ListRightPushAsync(key, log);
            await _db.KeyExpireAsync(key, LogTtl);
        }

        public async Task<List<string>> GetLogsAsync(string key)
        {
            var logs = await _db.ListRangeAsync(key);
            return logs.Select(x => x.ToString()).ToList();
        }

        public async Task DeleteLogsAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task DeleteKeyAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<List<string>> SearchKeysAsync(string pattern)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints[0]);

            var keys = server.Keys(pattern: pattern).ToList();
            return keys.Select(k => k.ToString()).ToList();
        }

        public async Task SetStringAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value, LogTtl);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task DeleteKeysByPatternAsync(string pattern)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints[0]);

            var keys = server.Keys(pattern: pattern);
            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
