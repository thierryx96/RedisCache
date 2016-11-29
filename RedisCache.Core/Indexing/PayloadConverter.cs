using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Indexing
{
    interface IIndexValueConverter<TValue>
    {
        string GetRedisValue(TValue item);
        TValue GetItem(TValue item);


    }
}
