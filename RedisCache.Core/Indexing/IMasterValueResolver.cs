using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal interface IMasterValueResolver<out TValue>
    {
        TValue[] GetMasterValues(IDatabase context, string indexedKey);
        TValue[] GetAllMasterValues(IDatabase context);
    }
}