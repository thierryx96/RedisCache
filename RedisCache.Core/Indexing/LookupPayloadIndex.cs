using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class LookupWithPayloadIndex<TValue> : IIndex<TValue>
    {
        private readonly ISerializer _serializer;
        private readonly TimeSpan? _expiry;
        public string Name { get; }
        public IKeyExtractor<TValue> Extractor { get; set; }

        private string GenerateSetName(string key) => $"{_indexCollectionPrefix}[{key}]";
        private readonly string _indexCollectionPrefix;

        public LookupWithPayloadIndex(
            string indexName,
            IKeyExtractor<TValue> valueExtractor,
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Name = indexName;
            Extractor = valueExtractor;

            _indexCollectionPrefix = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _serializer = serializer;
            _expiry = expiry;
        }

        public async Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
        {
            var setKey = GenerateSetName(indexedKey);
            var jsonValues = await context.SetMembersAsync(setKey);
            return jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue)).ToArray();
        }

        public async Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys)
        {
            IDictionary<string, TValue[]> indexedGroups = new Dictionary<string, TValue[]>();
            foreach (var indexedKey in indexedKeys)
            {
                indexedGroups[indexedKey] = await GetMasterValuesAsync(context, indexedKey);
            }
            return indexedGroups;
        }

        public TValue[] GetMasterValues(IDatabase context, string indexedKey)
        {
            var setKey = GenerateSetName(indexedKey);
            var jsonValues = context.SetMembers(setKey);
            return jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue)).ToArray();
        }

        public void AddOrUpdate(IDatabaseAsync context, TValue newItem, TValue oldItem)
        {
            var oldKey = oldItem != null ? Extractor.ExtractKey(oldItem) : null;
            var newKey = newItem != null ? Extractor.ExtractKey(newItem) : null;

            if (oldKey != null && !oldKey.Equals(newKey, StringComparison.OrdinalIgnoreCase))
            {
                Remove(context, new[] { oldItem });
            }

            if (newKey != null)
            {
                Set(context, new[] { newItem });
            }
        }

        public void Remove(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                context.SetRemoveAsync(GenerateSetName(Extractor.ExtractKey(item)), _serializer.Serialize(item));
            }
        }

        public void Set(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            var indexEntries = items.ToSets(Extractor.ExtractKey, item => _serializer.Serialize(item)).ToArray();
            foreach (var entry in indexEntries)
            {
                context.SetAddAsync(GenerateSetName(entry.Key), entry.ToArray());
                context.KeyExpireAsync(entry.Key, _expiry);
            }
        }
    }
}