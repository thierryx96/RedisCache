namespace PEL.Framework.Redis.Extractors
{
    /// <summary>
    /// Define a string value extraction from a collection's entity
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public interface IKeyExtractor<in TValue>
    {
        string ExtractKey(TValue value);
    }
}