using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Readers;
using PEL.Framework.Redis.Indexing.Writers;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Indexes
{
    internal class LookupKeyedIndex<TValue> : IIndex<TValue>
    {
        private readonly LookupIndexReader<string> _indexValueReader;
        private readonly IndexWriter<TValue> _indexWriter;
        private readonly Func<string, TValue> _masterValueGetter;
        private readonly string _indexCollectionPrefix;

        public LookupKeyedIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            Func<string, TValue> masterValueGetter,
            TimeSpan? expiry)
        {
            _indexCollectionPrefix = indexName;
            _masterValueGetter = masterValueGetter;
            Extractor = indexedKeyExtractor;


            _indexWriter = new LookupIndexWriter<TValue>(
                Extractor,
                masterKeyExtractor.ExtractKey,
                expiry,
                GenerateSetName);

            _indexValueReader = new LookupIndexReader<string>(GenerateSetName, value => value);
        }

        public async Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
            => (await GetMasterKeysAsync(context, indexedKey)).Select(_masterValueGetter).ToArray();

        public async Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            =>
                (await GetMasterKeysAsync(context, indexedKeys)).ToDictionary(value => value.Key,
                    value => value.Value.Select(_masterValueGetter).ToArray());

        public async Task<string[]> GetMasterKeysAsync(IDatabaseAsync context, string value)
            => await _indexValueReader.GetAsync(context, value);

        public async Task<IDictionary<string, string[]>> GetMasterKeysAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
            => await _indexValueReader.GetAsync(context, indexedKeys);

        public IKeyExtractor<TValue> Extractor { get; set; }
        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Remove(context, items);
        public void Set(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Set(context, items);
        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
            => _indexWriter.AddOrUpdate(context, newItem, oldItem);

        private string GenerateSetName(string indexedKey) => $"{_indexCollectionPrefix}[{indexedKey}]";
    }
}