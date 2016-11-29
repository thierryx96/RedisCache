using System;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Writers;
using PEL.Framework.Redis.Serialization;

namespace PEL.Framework.Redis.Indexing
{
    internal class IndexFactory<TValue>
    {
        private readonly TimeSpan? _expiry;
        private readonly string _masterCollectionRootName;
        private readonly ISerializer _serializer;

        internal IndexFactory(
            string masterCollectionRootName,
            TimeSpan? expiry,
            ISerializer serializer
        )
        {
            _serializer = serializer;
            _expiry = expiry;
            _masterCollectionRootName = masterCollectionRootName;
        }

        internal IIndexWriter<TValue> CreateIndex<TExtractor>(
            bool unique,
            bool withPayload,
            TExtractor indexedKeyExtractor,
            string name)
            where TExtractor : IKeyExtractor<TValue>
        {
            if (withPayload && unique)
            {
                return new UniquePayloadIndex<TValue>(
                    name ?? indexedKeyExtractor.GetType().Name,
                    indexedKeyExtractor,
                    _masterCollectionRootName,
                    _serializer,
                    _expiry);
            }
            else if (withPayload)
            {
                return new LookupPayloadIndex<TValue>(
                    name ?? indexedKeyExtractor.GetType().Name,
                    indexedKeyExtractor,
                    _masterCollectionRootName,
                    _serializer,
                    _expiry);
            }

            // TODO: (trais, 21 Nov 2016) - implement other type of possible indexes : keyed unique & keyed lookup 
            throw new NotImplementedException($"Keyed indexes are not supported yet.");
        }
    }
}