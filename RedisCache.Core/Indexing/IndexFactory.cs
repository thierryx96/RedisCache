using System;
using PEL.Framework.Redis.Configuration;
using PEL.Framework.Redis.Extractors;

namespace PEL.Framework.Redis.Indexing
{
    public class IndexFactory<TValue>
    {
        private readonly TimeSpan? _expiry;
        private readonly string _masterCollectionRootName;
        private readonly IKeyExtractor<TValue> _masterKeyResolver;
        private readonly IValueExtractorAsync<TValue> _masterValueExtractor;

        public IndexFactory(
            IKeyExtractor<TValue>  masterKeyResolver,
            IValueExtractorAsync<TValue> masterValueExtractor,
            string masterCollectionRootName,
            TimeSpan? expiry)
        {
            _masterKeyResolver = masterKeyResolver;
            _masterValueExtractor = masterValueExtractor;
            _expiry = expiry;
            _masterCollectionRootName = masterCollectionRootName;
        }

        public IIndex<TValue> CreateIndex<TExtractor>(bool unique, bool withPayload, TExtractor indexValueExtractor, string name = null)
            where TExtractor : IKeyExtractor<TValue>
        {

            if (withPayload)
            {
                if (unique)
                {
                    (IIndex<TValue>)new UniquePayloadIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor, _masterValueExtractor, _masterCollectionRootName, _expiry) :

                }
                throw new NotImplementedException();
            }
            return unique ?
            (IIndex<TValue>)new UniqueIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor, _masterKeyExtractor,  _masterCollectionRootName, _expiry) :
            (IIndex<TValue>)new MapIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor, _masterKeyExtractor,  _masterCollectionRootName, _expiry);
        }


    }
}
