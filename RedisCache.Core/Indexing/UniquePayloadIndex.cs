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
    public class UniquePayloadIndex<TValue> : IIndex<TValue>
    {
        private TimeSpan? _expiry;
        public string Name { get; private set; }
        public IKeyExtractor<TValue> Extractor { get; set; }
        public IKeyExtractor<TValue> _masterKeyExtractor;

        public string _hashIndexCollectionName;
        private readonly ISerializer _serializer;

        public UniquePayloadIndex(
            string indexName,
            IKeyExtractor<TValue> valueExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Name = indexName;
            Extractor = valueExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
            _serializer = serializer;
        }

        public async Task AddOrUpdateAsync(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !Extractor.ExtractKey(newItem).Equals(Extractor.ExtractKey(oldItem)))
            {
                RemoveAsync(context, new[] { oldItem });
            }
            SetAsync(context, new[] { newItem });
        }


        public async Task<TValue> GetValue(IDatabaseAsync context, string key)
        {
            var jsonValue = await context.HashGetAsync(_hashIndexCollectionName, key);
            if (!jsonValue.HasValue) return default(TValue);
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return item;
        }

        public virtual async Task<IEnumerable<TValue>> GetAllValues(IDatabaseAsync context)
        {
            var jsonValues = await context.HashValuesAsync(_hashIndexCollectionName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public async Task RemoveAsync(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_hashIndexCollectionName, items.Select(Extractor.ExtractKey).ToHashKeys());
        }

        public async Task ClearAsync(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(_hashIndexCollectionName);
        }

        public async Task SetAsync(
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

        public void Set(
            IDatabase context,
            IEnumerable<TValue> items
            )
        {
            var indexName = _hashIndexCollectionName;
            context.HashSet(indexName, items.ToHashEntries(Extractor.ExtractKey, item => _serializer.Serialize(item)));

            if (_expiry.HasValue)
            {
                context.KeyExpire(indexName, _expiry);
            }
        }


    }
}
