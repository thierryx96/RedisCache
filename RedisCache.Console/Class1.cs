using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console
{
    public class CompanyChangedEventHandler : IEventHandler<CacheSourceChanged>
    {
        // InMemory cache wrapper instance
        private readonly ICacheStore _cacheStore;

        public BookChangedEventHandler(ICacheStore cacheStore)
        {
            _cacheStore = cacheStore;
        }

        public void Handle(CacheSourceChanged eEvent)
        {
            // Remove cahce data from memory so next time the category menu load from original source
            _cacheStore.Remove("categories:menu");
        }
    }
}
