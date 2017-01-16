using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Indexing.Readers
{
    public class LookupIndexReader<TIndexedValue>
    {
        private readonly Func<string, string> _indexCollectionNameGenerator;
        private readonly Func<string, TIndexedValue> _indexedValueReader;

        public LookupIndexReader(
            Func<string, string> indexCollectionNameGenerator,
            Func<string, TIndexedValue> indexedValueReader // json -> something
        )
        {
            _indexCollectionNameGenerator = indexCollectionNameGenerator;
            _indexedValueReader = indexedValueReader;
        }

        private string GenerateSetName(string indexedKey) => _indexCollectionNameGenerator(indexedKey);
        // $"{_indexCollectionPrefix}[{indexedKey}]";

        public async Task<TIndexedValue[]> GetAsync(IDatabaseAsync context, string indexedKey)
        {
            var setKey = GenerateSetName(indexedKey);
            var jsonValues = await context.SetMembersAsync(setKey);
            return jsonValues.Select(jsonValue => _indexedValueReader(jsonValue)).ToArray();
        }

        public async Task<IDictionary<string, TIndexedValue[]>> GetAsync(IDatabaseAsync context,
            IEnumerable<string> indexedKeys)
        {
            IDictionary<string, TIndexedValue[]> indexedGroups = new Dictionary<string, TIndexedValue[]>();
            foreach (var indexedKey in indexedKeys)
                indexedGroups[indexedKey] = await GetAsync(context, indexedKey);
            return indexedGroups;
        }
    }
}