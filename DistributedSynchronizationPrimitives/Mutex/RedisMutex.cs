namespace DistributedSynchronizationPrimitives.Mutex
{
    using StackExchange.Redis;

    public class RedisMutex
    {
        ConnectionMultiplexer connection;

        public RedisMutex(string connectionString)
        {
            this.connection = ConnectionMultiplexer.Connect(connectionString);
        }

        public bool Acquire(string key, int expire)
        {
            RedisValue strExpire = expire.ToString();
            var script = "local locked = redis.call('SETNX', KEYS[1], ARGV[1]);" +
                "if locked == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[2]) end;" +
                "return locked";
            IDatabase database = connection.GetDatabase();
            RedisResult result = database.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { key, strExpire });
            return (bool)result;
        }

        public void Release(string key)
        {
            var script = "local id = redis.call('GET', KEYS[1]);" +
                "if id == ARGV[1] then redis.call('DEL', KEYS[1]) end";
            IDatabase database = connection.GetDatabase();
            RedisResult result = database.ScriptEvaluate(script, new RedisKey[] { key }, new RedisValue[] { key });
        }
    }
}