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
    public class UniquePayloadIndex<TValue> : IIndex<TValue>, IMasterValueResolver<TValue>, IMasterKeyResolver
    {
        private readonly Func<TValue, string> _masterKeyExtractor;
        private TimeSpan? _expiry;
        public string Name { get; }
        public IKeyExtractor<TValue> Extractor { get; set; }

        private readonly string _hashIndexCollectionName;
        private readonly ISerializer _serializer;

        public UniquePayloadIndex(
            string indexName,
            IKeyExtractor<TValue> valueExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {
            Name = indexName;
            Extractor = valueExtractor;
            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _masterKeyExtractor = masterKeyExtractor;
            _expiry = expiry;
            _serializer = serializer;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && !Extractor.ExtractKey(newItem).Equals(Extractor.ExtractKey(oldItem)))
            {
                Remove(context, new[] { oldItem });
            }
            Set(context, new[] { newItem });
        }


        public async Task<IEnumerable<TValue>> GetMasterValues(IDatabaseAsync context, string key)
        {
            var jsonValue = await context.HashGetAsync(_hashIndexCollectionName, key);
            if (!jsonValue.HasValue) return new TValue[]{};
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return new [] { item }; 
        }

        public virtual async Task<IEnumerable<TValue>> GetAllMasterValues(IDatabaseAsync context)
        {
            var jsonValues = await context.HashValuesAsync(_hashIndexCollectionName);
            var items = jsonValues.Select(jsonValue => _serializer.Deserialize<TValue>(jsonValue));
            return items;
        }

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string key)
        {
            var values = await GetMasterValues(context, key);
            return values.Select(_masterKeyExtractor);
        }

        public async Task<IEnumerable<string>> GetAllMasterKeys(IDatabaseAsync context)
        {
            var values = await GetAllMasterValues(context);
            return values.Select(_masterKeyExtractor);
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            context.HashDeleteAsync(_hashIndexCollectionName, items.Select(Extractor.ExtractKey).ToHashKeys());
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            context.KeyDeleteAsync(_hashIndexCollectionName);
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
