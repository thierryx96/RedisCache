namespace PEL.Framework.Redis.Extractors
{
    public interface IKeyExtractor<in TValue>
    {
        string ExtractKey(TValue value);
    }
}