using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Readers;
using PEL.Framework.Redis.Indexing.Writers;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal class UniquePayloadIndex<TValue> : IIndex<TValue>
    {
        private readonly UniqueIndexReader<TValue> _indexValueReader;
        private readonly IndexWriter<TValue> _indexWriter;
        private readonly IKeyExtractor<TValue> _masterKeyExtractor;

        public UniquePayloadIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Extractor = indexedKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            var hashIndexCollectionName = indexName; //$"{collectionRootName}:{indexName.ToLowerInvariant()}";

            _indexWriter = new UniqueIndexWriter<TValue>(
                Extractor,
                serializer.Serialize,
                expiry,
                hashIndexCollectionName);

            _indexValueReader = new UniqueIndexReader<TValue>(hashIndexCollectionName, serializer.Deserialize<TValue>);
        }


        /// <summary>
        ///     Get values from index (in the case of a unique index, only one or 0 values should be returned)
        /// </summary>
        public async Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
            => (await _indexValueReader.GetAsync(context, indexedKey)).ToUnitOrEmpty();

        public async Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            =>
                (await _indexValueReader.GetAsync(context, indexedKeys)).ToDictionary(value => value.Key,
                    value => value.Value.ToUnitOrEmpty());

        public async Task<string[]> GetMasterKeysAsync(IDatabaseAsync context, string value)
            => (await GetMasterValuesAsync(context, value)).Select(_masterKeyExtractor.ExtractKey).ToArray();

        public async Task<IDictionary<string, string[]>> GetMasterKeysAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            =>
                (await GetMasterValuesAsync(context, indexedKeys)).ToDictionary(value => value.Key,
                    value => value.Value.Select(_masterKeyExtractor.ExtractKey).ToArray());

        public IKeyExtractor<TValue> Extractor { get; set; }
        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Remove(context, items);
        public void Set(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Set(context, items);

        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
            => _indexWriter.AddOrUpdate(context, newItem, oldItem);
    }
}