using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using PEL.Framework.Redis.Extractors;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing
{
    public class LookupKeyIndex<TValue> : IIndex<TValue>, IMasterKeyResolver
    {
        private readonly TimeSpan? _expiry;
        public string Name { get; private set; }
        public IKeyExtractor<TValue> Extractor { get; set; }
        private readonly IKeyExtractor<TValue> _masterKeyExtractor;

        //public  string Extractor(TValue value) => _indexKeyExtractor(value);

        private string GenerateSetCollectionName(string key) => $"{_indexCollectionPrefix}[{key}]";
        private readonly string _indexCollectionPrefix;

        public LookupKeyIndex(
            string indexName,
            IKeyExtractor<TValue> indexKeyExtractor,
            IKeyExtractor<TValue> masterKeyExtractor,
            string collectionRootName,
            TimeSpan? expiry)
        {
            Name = indexName;

            Extractor = indexKeyExtractor;
            _masterKeyExtractor = masterKeyExtractor;

            _indexCollectionPrefix = $"{collectionRootName}:{indexName.ToLowerInvariant()}";
            _expiry = expiry;
        }

        public void AddOrUpdate(
            IDatabaseAsync context,
            TValue newItem,
            TValue oldItem)
        {
            if (oldItem != null && Extractor.ExtractKey(oldItem)!= null &&  !Extractor.ExtractKey(newItem).Equals(Extractor.ExtractKey(oldItem)))
            {
                Remove(context, new[] { oldItem });
            };

            if(Extractor.ExtractKey(newItem) != null)
            {
                Set(context, new[] { newItem });
            }
        }

        public async Task<IEnumerable<string>> GetMasterKeys(IDatabaseAsync context, string indexKey)
        {
            var masterKeys = await context.SetMembersAsync(GenerateSetCollectionName(indexKey));
            return masterKeys.Select(masterKey => masterKey.ToString()).ToArray();
        }

        public Task<IEnumerable<string>> GetAllMasterKeys(IDatabaseAsync context)
        {
            throw new NotImplementedException();
        }

        public void Remove(
            IDatabaseAsync context,
            IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                context.SetRemoveAsync(GenerateSetCollectionName(Extractor.ExtractKey(item)), _masterKeyExtractor.ExtractKey(item));
            }
        }

        public void Clear(
            IDatabaseAsync context
            )
        {
            // Not at problem if the index is stale as long as the master key is removed.
        }

        public void RemoveAsync(IDatabaseAsync context, IEnumerable<TValue> items)
        {
            throw new NotImplementedException();
        }

        public void Set(
            IDatabaseAsync context,
            IEnumerable<TValue> items
            )
        {
            var indexEntries = items.ToSets(Extractor.ExtractKey, _masterKeyExtractor.ExtractKey).ToArray();

            foreach (var entry in indexEntries)
            {
                context.SetAddAsync(GenerateSetCollectionName(entry.Key), entry.ToArray());
                context.KeyExpireAsync(entry.Key, _expiry);
            }        
        }


    }

}
