using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console.Messaging
{
    public class UpdateMessage<T> 
    {
        public string Key { get; set; }
        public T Data { get; set; }
    }
}
