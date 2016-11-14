using System.Collections.Generic;
using System.Threading.Tasks;
using PEL.ES.Infrastructure.Caching.Serialization;
using StackExchange.Redis;

namespace PEL.ES.Infrastructure.Caching.Publishing
{
    public class RedisDataChangeEventPublisher : IDataChangeEventPublisher
    {
        private readonly ISerializer _serializer;
        private readonly ISubscriber _publisher;

        public RedisDataChangeEventPublisher(IConnectionMultiplexer redisPublisherConnection, ISerializer serializer)
        {
            _serializer = serializer;
            _publisher = redisPublisherConnection.GetSubscriber();
        }


        public async Task PublishAdded<TData>(string key, TData addedItem)
        {
            var channel = $"{typeof(TData).Name}:{typeof(ItemAddedMessage<TData>).Name}";
            var message = new ItemAddedMessage<TData>() {Id = key, Item = addedItem} ;

            await _publisher.PublishAsync(channel, _serializer.Serialize(message));
        }

        public async Task PublishUpdated<TData>(string key, TData updatedItem)
        {
            var channel = $"{typeof(TData).Name}:{typeof(ItemUpdatedMessage<TData>).Name}";
            var message = new ItemUpdatedMessage<TData>() { Id = key, Item = updatedItem };

            await _publisher.PublishAsync(channel, _serializer.Serialize(message));
        }

        public async Task PublishDeleted<TData>(string key)
        {
            var channel = $"{typeof(TData).Name}:{typeof(ItemDeletedMessage<TData>).Name}";
            var message = new ItemDeletedMessage<TData>(){ Id = key };

            await _publisher.PublishAsync(channel, _serializer.Serialize(message));
        }

        public async Task PublishInitLoadNeeded<TData>(IDictionary<string,TData> items)
        {
            var channel = $"{typeof(TData).Name}:{typeof(InitialLoadNeededMessage<TData>).Name}";
            var message = new InitialLoadNeededMessage<TData>() { Items = items };

            await _publisher.PublishAsync(channel, _serializer.Serialize(message));
        }
    }
}