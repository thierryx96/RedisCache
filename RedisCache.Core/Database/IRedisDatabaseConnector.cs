using StackExchange.Redis;

namespace PEL.Framework.Redis.Database
{
    public interface IRedisDatabaseConnector
    {
        IDatabase GetConnectedDatabase();
    }
}