using System;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Indexes;
using PEL.Framework.Redis.Serialization;

namespace PEL.Framework.Redis.Indexing
{
    internal class IndexFactory<TValue>
    {
        private readonly TimeSpan? _expiry;
        private readonly string _masterCollectionRootName;
        private readonly IKeyExtractor<TValue> _masterKeyExtractor;
        private readonly ISerializer _serializer;

        internal IndexFactory(
            string masterCollectionRootName,
            TimeSpan? expiry,
            ISerializer serializer,
            IKeyExtractor<TValue> masterKeyExtractor
        )
        {
            _serializer = serializer;
            _masterKeyExtractor = masterKeyExtractor;
            _expiry = expiry;
            _masterCollectionRootName = masterCollectionRootName;
        }

        internal IIndex<TValue> CreateKeyedIndex<TExtractor>(
            bool unique,
            TExtractor indexedKeyExtractor,
            Func<string, TValue> masterValueGetter,
            string name)
            where TExtractor : IKeyExtractor<TValue>
        {
            var indexName = $"{_masterCollectionRootName}:{name ?? indexedKeyExtractor.GetType().Name}";

            if (unique)
                return new UniqueKeyedIndex<TValue>(
                    indexName,
                    indexedKeyExtractor,
                    _masterKeyExtractor,
                    masterValueGetter,
                    _expiry);
            return new LookupPayloadIndex<TValue>(
                indexName,
                indexedKeyExtractor,
                _masterKeyExtractor,
                _serializer,
                _expiry);
        }

        internal IIndex<TValue> CreatePayloadIndex<TExtractor>(
            bool unique,
            TExtractor indexedKeyExtractor,
            string name)
            where TExtractor : IKeyExtractor<TValue>
        {
            var indexName = $"{_masterCollectionRootName}:{name ?? indexedKeyExtractor.GetType().Name}";

            if (unique)
                return new UniquePayloadIndex<TValue>(
                    indexName,
                    indexedKeyExtractor,
                    _masterKeyExtractor,
                    _serializer,
                    _expiry);
            return new LookupPayloadIndex<TValue>(
                indexName,
                indexedKeyExtractor,
                _masterKeyExtractor,
                _serializer,
                _expiry);
        }
    }
}