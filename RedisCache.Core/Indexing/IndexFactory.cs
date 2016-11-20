using System;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Serialization;

namespace PEL.Framework.Redis.Indexing
{
    public class IndexFactory<TValue>
    {
        private readonly TimeSpan? _expiry;
        private readonly string _masterCollectionRootName;
        private readonly IKeyExtractor<TValue> _masterKeyResolver;

        private readonly ISerializer _serializer;
        //private Func<string, TValue> _masterValueExtractor;


        public IndexFactory(
            ISerializer serializer,
            IKeyExtractor<TValue> masterKeyResolver,
            string masterCollectionRootName,
            TimeSpan? expiry)
        {
            _serializer = serializer;
            _masterKeyResolver = masterKeyResolver;
            _expiry = expiry;
            _masterCollectionRootName = masterCollectionRootName;
        }

        public IIndex<TValue> CreateIndex<TExtractor>(bool unique, bool withPayload, TExtractor indexValueExtractor,
            string name = null)
            where TExtractor : IKeyExtractor<TValue>
        {
            if (withPayload)
            {
                if (unique)
                    return new UniquePayloadIndex<TValue>(
                        name ?? indexValueExtractor.GetType().Name,
                        indexValueExtractor,
                        _masterKeyResolver.ExtractKey,
                        _masterCollectionRootName,
                        _serializer,
                        _expiry);
                throw new NotImplementedException();
            }
            if (unique)
                return new UniqueKeyIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor,
                    _masterKeyResolver, _masterCollectionRootName, _expiry);
            return new LookupKeyIndex<TValue>(name ?? indexValueExtractor.GetType().Name, indexValueExtractor,
                _masterKeyResolver, _masterCollectionRootName, _expiry);
        }
    }
}