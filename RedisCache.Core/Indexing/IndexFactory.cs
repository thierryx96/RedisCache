using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Indexing
{
    public class IndexFactory<TValue>
    {
        private TimeSpan? _expiry;
        private string _masterCollectionRootName;
        private Func<TValue, string> _masterKeyExtractor;

        public IndexFactory(
            Func<TValue, string> masterKeyExtractor, 
            string masterCollectionRootName,
            TimeSpan? expiry)
        {
            _expiry = expiry;
            _masterKeyExtractor = masterKeyExtractor;
            _masterCollectionRootName = masterCollectionRootName;
        }

        public IIndex<TValue> CreateIndex(IndexDefinition<TValue> definition)
        {
            return definition.Unique ?
            (IIndex<TValue>)new UniqueIndex<TValue>(definition.Name, definition.KeyExtractor, _masterKeyExtractor, _masterCollectionRootName, _expiry) :
            (IIndex<TValue>)new MapIndex<TValue>(definition.Name, definition.KeyExtractor, _masterKeyExtractor, _masterCollectionRootName, _expiry);
        }
    }
}
