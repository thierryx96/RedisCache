using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using PEL.Framework.Redis.Indexing.Writers;
using PEL.Framework.Redis.Serialization;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class LookupPayloadIndex<TValue> : IIndexWriter<TValue>
    {
        private readonly ISerializer _serializer;
        private readonly TimeSpan? _expiry;
        public string Name { get; }
        public IKeyExtractor<TValue> IndexedKeyExtractor { get; set; }

        private string GenerateSetName(string indexedKey) => $"{_indexCollectionPrefix}[{indexedKey}]";
        private readonly string _indexCollectionPrefix;

        public LookupPayloadIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Name = indexName;
            IndexedKeyExtractor = indexedKeyExtractor;
            _indexCollectionPrefix = $"{collectionRootName}:{Name}";
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

        public IDictionary<string, TValue[]> GetMasterValues(IDatabase context, IEnumerable<string> indexedKeys)
        {
            IDictionary<string, TValue[]> indexedGroups = new Dictionary<string, TValue[]>();
            foreach (var indexedKey in indexedKeys)
            {
                indexedGroups[indexedKey] = GetMasterValues(context, indexedKey);
            }
            return indexedGroups;
        }

        
    }
}