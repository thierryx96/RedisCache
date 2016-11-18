using System.Threading.Tasks;

namespace PEL.Framework.Redis.Extractors
{
    public interface IValueExtractorAsync<TValue>
    {
        Task<TValue> ExtractValueAsync(string key);
    }
}