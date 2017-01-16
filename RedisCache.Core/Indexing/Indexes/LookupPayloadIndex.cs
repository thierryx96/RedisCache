using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Readers;
using PEL.Framework.Redis.Indexing.Writers;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class LookupPayloadIndex<TValue> : IIndex<TValue>
    {
        private readonly string _indexCollectionPrefix;
        private readonly LookupIndexReader<TValue> _indexValueReader;
        private readonly IndexWriter<TValue> _indexWriter;
        private readonly IKeyExtractor<TValue> _masterKeyExtractor;

        public LookupPayloadIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            _indexCollectionPrefix = indexName;
            _masterKeyExtractor = masterKeyExtractor;
            Extractor = indexedKeyExtractor;

            _indexWriter = new LookupIndexWriter<TValue>(
                Extractor,
                serializer.Serialize,
                expiry,
                GenerateSetName);

            _indexValueReader = new LookupIndexReader<TValue>(GenerateSetName, serializer.Deserialize<TValue>);
        }

        public IKeyExtractor<TValue> Extractor { get; set; }

        /// <summary>
        ///     Get values from index (in the case of a unique index, only one or 0 values should be returned)
        /// </summary>
        public Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
            => _indexValueReader.GetAsync(context, indexedKey);

        public Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            => _indexValueReader.GetAsync(context, indexedKeys);

        public async Task<string[]> GetMasterKeysAsync(IDatabaseAsync context, string value)
            => (await GetMasterValuesAsync(context, value)).Select(_masterKeyExtractor.ExtractKey).ToArray();

        public async Task<IDictionary<string, string[]>> GetMasterKeysAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            =>
                (await GetMasterValuesAsync(context, indexedKeys)).ToDictionary(value => value.Key,
                    value => value.Value.Select(_masterKeyExtractor.ExtractKey).ToArray());

        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Remove(context, items);
        public void Set(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Set(context, items);

        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
            => _indexWriter.AddOrUpdate(context, newItem, oldItem);

        private string GenerateSetName(string indexedKey) => $"{_indexCollectionPrefix}[{indexedKey}]";
    }
}