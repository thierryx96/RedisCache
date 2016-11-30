using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Readers;
using PEL.Framework.Redis.Indexing.Writers;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Indexes
{
    internal class UniqueKeyedIndex<TValue> : IIndex<TValue>
    {
        private readonly Func<string,TValue> _masterValueGetter;
        private readonly IndexWriter<TValue> _indexWriter;
        private readonly UniqueIndexReader<string> _indexValueReader;

        public UniqueKeyedIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            Func<string, TValue> masterValueGetter, 
            TimeSpan? expiry)
        {
            Extractor = indexedKeyExtractor;
            _masterValueGetter = masterValueGetter;
            string hashIndexCollectionName = indexName;

            _indexWriter = new UniqueIndexWriter<TValue>(
                Extractor,
                masterKeyExtractor.ExtractKey,
                expiry,
                hashIndexCollectionName);

            _indexValueReader = new UniqueIndexReader<string>(hashIndexCollectionName, value => value);
        }

        public async Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
            => (await GetMasterKeysAsync(context, indexedKey)).Select(key => _masterValueGetter(key)).ToArray();

        public async Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys) 
            => (await GetMasterKeysAsync(context, indexedKeys)).ToDictionary(value => value.Key, value => value.Value.Select(_masterValueGetter).ToArray());

        public async Task<string[]> GetMasterKeysAsync(IDatabaseAsync context, string value)
            => (await _indexValueReader.GetAsync(context, value)).ToUnitOrEmpty();

        public async Task<IDictionary<string, string[]>> GetMasterKeysAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys)
            => (await _indexValueReader.GetAsync(context, indexedKeys)).ToDictionary(value => value.Key, value => value.Value.ToUnitOrEmpty());

        public IKeyExtractor<TValue> Extractor { get; set; }
        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Remove(context, items);
        public void Set(IDatabaseAsync context, IEnumerable<TValue> items) => _indexWriter.Set(context, items);
        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem) => _indexWriter.AddOrUpdate(context, newItem, oldItem);

    }
}