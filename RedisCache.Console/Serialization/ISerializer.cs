using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console.Serialization
{
    public interface ISerializer
    {
        string Serialize<T>(T data);
        T Deserialize<T>(string serializedData);
    }

}
