using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RedisCache.Console.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console.Serialization
{

    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            _settings.Converters.Add(new StringEnumConverter());
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        public T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, _settings);
        }
    }
}
