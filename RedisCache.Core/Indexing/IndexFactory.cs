using System;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Indexing
{
    public class IndexFactory<TValue>
    {
        private readonly TimeSpan? _expiry;
        private readonly string _masterCollectionRootName;
        private readonly Func<TValue, string> _masterKeyExtractor;

        public IndexFactory(
            Func<TValue, string> masterKeyExtractor, 
            string masterCollectionRootName,
            TimeSpan? expiry)
        {
            _expiry = expiry;
            _masterKeyExtractor = masterKeyExtractor;
            _masterCollectionRootName = masterCollectionRootName;
        }

        public IIndex<TValue> CreateIndex<TExtractor>(bool unique, TExtractor indexValueExtractor, string name = null)
            where TExtractor : IKeyExtractor<TValue>
        {
            return unique ?
            (IIndex<TValue>)new UniqueIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor, _masterKeyExtractor, _masterCollectionRootName, _expiry) :
            (IIndex<TValue>)new MapIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor, _masterKeyExtractor, _masterCollectionRootName, _expiry);
        }
    }
}
