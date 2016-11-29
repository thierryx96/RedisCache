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
    internal class UniquePayloadIndex<TValue> 
    {
        public string Name { get; }
        public IKeyExtractor<TValue> IndexedKeyExtractor { get; set; }

        internal IIndexWriter<TValue> IndexWriter { get; set; }
        private readonly string _hashIndexCollectionName;
        private readonly ISerializer _serializer;

        public UniquePayloadIndex(
            string indexName,
            IKeyExtractor<TValue> indexedKeyExtractor,            
            string collectionRootName,
            ISerializer serializer,
            TimeSpan? expiry)
        {

            

            Name = indexName;
            IndexedKeyExtractor = indexedKeyExtractor;
            _hashIndexCollectionName = $"{collectionRootName}:{indexName.ToLowerInvariant()}";

            IndexWriter = new UniqueIndexWriter<TValue>(
                indexedKeyExtractor,

                _hashIndexCollectionName,
                expiry);

            _expiry = expiry;
            _serializer = serializer;
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
                .ToDictionary(item => IndexedKeyExtractor.ExtractKey(item), item => new[] { item });
        }

        public TValue[] GetMasterValues(IDatabase context, string indexedKey)
        {
            var jsonValue = context.HashGet(_hashIndexCollectionName, indexedKey);
            if (!jsonValue.HasValue) return new TValue[] { };
            var item = _serializer.Deserialize<TValue>(jsonValue);
            return new[] { item };
        }


        public IDictionary<string, TValue[]> GetMasterValues(IDatabase context, IEnumerable<string> indexedKeys)
        {
            var jsonValues = context.HashGet(_hashIndexCollectionName, indexedKeys.ToHashKeys());
            return jsonValues
                .Where(entry => entry.HasValue)
                .Select(entry => _serializer.Deserialize<TValue>(entry))
                .ToDictionary(item => IndexedKeyExtractor.ExtractKey(item), item => new[] { item });
        }


        

    }
}