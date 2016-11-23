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
    internal class UniquePayloadIndex<TValue> : IIndex<TValue>
    {
        private TimeSpan? _expiry;
        public string Name { get; }
        public IKeyExtractor<TValue> Extractor { get; set; }

        private readonly string _hashIndexCollectionName;
        private readonly ISerializer _serializer;

        public UniquePayloadIndex(
            string indexName,
            IKeyExtractor<TValue> valueExtractor,
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Name = indexName;
            Extractor = valueExtractor;
            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
            _serializer = serializer;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !Extractor.ExtractKey(newItem).Equals(Extractor.ExtractKey(oldItem), StringComparison.OrdinalIgnoreCase))
            {
                Remove(context, new[] { oldItem });
            }
            Set(context, new[] { newItem });
        }

        /// <summary>
        /// Get values from index (in the case of a unique index, only one or 0 values should be returned)
        /// </summary>
        public async Task<TValue[]> GetMasterValuesAsync(IDatabaseAsync context, string indexedKey)
        {
            var jsonValue = await context.HashGetAsync(_hashIndexCollectionName, indexedKey);
            if (!jsonValue.HasValue) return new TValue[] { };
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return new[] { item };
        }

        public async Task<IDictionary<string, TValue[]>> GetMasterValuesAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys)
        {
            var jsonValues = await context.HashGetAsync(_hashIndexCollectionName, indexedKeys.ToHashKeys());
            return jsonValues
                .Where(entry => entry.HasValue)
                .Select(entry => _serializer.Deserialize<TValue>(entry))
                .ToDictionary(item => Extractor.ExtractKey(item), item => new[] { item });
        }

        public TValue[] GetMasterValues(IDatabase context, string key)
        {
            var jsonValue = context.HashGet(_hashIndexCollectionName, key);
            if (!jsonValue.HasValue) return new TValue[] { };
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return new[] { item };
        }

        public TValue[] GetAllMasterValues(IDatabase context)
        {
            var jsonValues = context.HashValues(_hashIndexCollectionName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue)).ToArray();
            return items;
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_hashIndexCollectionName, items.Select(Extractor.ExtractKey).ToHashKeys());
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
        )
        {
            var indexName = _hashIndexCollectionName;
            context.HashSetAsync(indexName, items.ToHashEntries(Extractor.ExtractKey, item => _serializer.Serialize(item)));

            if (_expiry.HasValue)
            {
                context.KeyExpireAsync(indexName, _expiry);
            }
        }
    }
}