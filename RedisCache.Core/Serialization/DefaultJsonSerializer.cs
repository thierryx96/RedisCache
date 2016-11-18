using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace PEL.Framework.Redis.Serialization
{
    internal class DefaultJsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings;

        public DefaultJsonSerializer(params JsonConverter[] converters)
        {
            _settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                TypeNameHandling = TypeNameHandling.Objects
            };
            _settings.Converters.Add(new StringEnumConverter());

            foreach (var converter in converters)
            {
                _settings.Converters.Add(converter);
            }
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