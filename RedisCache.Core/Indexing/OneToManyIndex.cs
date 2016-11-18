using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    internal class OneToManyIndex<TValue> : IIndex<TValue>
    {
        private readonly TimeSpan? _expiry;
        public string Name { get; }
        public IKeyExtractor<TValue> Extractor { get; set; }
        public Func<TValue, string> _indexKeyExtractor;
        public Func<TValue, string> ValueExtractor => _indexKeyExtractor;

        private string GenerateSetCollectionName(string key) => $"{_indexCollectionPrefix}[{key}]";
        private readonly Func<TValue, string> _masterKeyExtractor;
        private readonly string _indexCollectionPrefix;

        public OneToManyIndex(
            string indexName,
            Func<TValue, string> indexValueExtractor,
            Func<TValue, string> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;
            _indexKeyExtractor = indexValueExtractor;
            _masterKeyExtractor = masterKeyExtractor;
            _indexCollectionPrefix = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && _indexKeyExtractor(oldItem) != null && !_indexKeyExtractor(newItem).Equals(_indexKeyExtractor(oldItem)))
            {
                Remove(context, new[] { oldItem });
            }
            ;

            if (_indexKeyExtractor(newItem) != null)
            {
                Set(context, new[] { newItem });
            }
        }

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string indexKey)
        {
            var masterKeys = await context.SetMembersAsync(GenerateSetCollectionName(indexKey));
            return masterKeys.Select(masterKey => masterKey.ToString()).ToArray();
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                context.SetRemoveAsync(GenerateSetCollectionName(_indexKeyExtractor(item)), _masterKeyExtractor(item));
            }
        }

        public void Clear(
            IDatabaseAsync context
        )
        {
            // // TODO: (trais, 10 Nov 2016) - find a way to clear, without a scan on the keys
            // Not at problem if the index is stale as long as the master key is removed.
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
        )
        {
            var indexEntries = items.ToSets(_indexKeyExtractor, _masterKeyExtractor).ToArray();

            foreach (var entry in indexEntries)
            {
                context.SetAddAsync(GenerateSetCollectionName(entry.Key), entry.ToArray());
                context.KeyExpireAsync(entry.Key, _expiry);
            }
        }
    }
}