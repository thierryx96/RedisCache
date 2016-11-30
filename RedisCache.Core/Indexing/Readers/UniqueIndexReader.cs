using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PEL.Framework.Redis.Extensions;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Readers
{
    internal class UniqueIndexReader<TIndexedValue>
    {
        private readonly string _indexCollectionName;
        private readonly Func<string, TIndexedValue> _indexedValueReader;

        public UniqueIndexReader(
            string indexCollectionName,
            Func<string, TIndexedValue> indexedValueReader)
        {
            _indexCollectionName = indexCollectionName;
            _indexedValueReader = indexedValueReader;
        }

        public async Task<TIndexedValue> GetAsync(IDatabaseAsync context, string indexedKey)
        {
            var jsonValue = await context.HashGetAsync(_indexCollectionName, indexedKey);
            return !jsonValue.HasValue ? default(TIndexedValue) : _indexedValueReader(jsonValue);
        }

        public async Task<IDictionary<string, TIndexedValue>> GetAsync(IDatabaseAsync context, IEnumerable<string> indexedKeys)
        {
            var keys = indexedKeys as string[] ?? indexedKeys.ToArray();
            var jsonValues = await context.HashGetAsync(_indexCollectionName, keys.ToHashKeys());
            var entries = new Dictionary<string, TIndexedValue>();
            for (int i = 0; i < keys.Count(); i++)
            {
                var jsonValue = jsonValues[i];
                entries[keys[i]] = !jsonValue.HasValue ? default(TIndexedValue) : _indexedValueReader(jsonValue);
            }
            return entries;
        }
    }
}