using System;
using StackExchange.Redis;

namespace PEL.Framework.Redis.Expiry
{
    namespace PEL.Framework.Redis.Publishing
    {
        public class RedisExpiryMessageSubscriber : IDisposable
        {
            private readonly ISubscriber _subscriber;
            private readonly string _keySpaceChannel;

            public static RedisExpiryMessageSubscriber CreateForCollectionType<TValue>(
                IConnectionMultiplexer redisPublisherConnection
            )
            {
                return new RedisExpiryMessageSubscriber(redisPublisherConnection, typeof(TValue).Name.ToLowerInvariant());
            }

            protected RedisExpiryMessageSubscriber(
                IConnectionMultiplexer redisPublisherConnection,
                string collectionName
            )
            {
                _subscriber = redisPublisherConnection.GetSubscriber();
                _keySpaceChannel = $"__keyspace@0__:{collectionName}*";
            }

            public void SubscribeExpiry(Action<string> onExpiryMessage)
            {
                _subscriber.Subscribe(_keySpaceChannel, (ctx, message) =>
                {
                    if ((string)message == "expired")
                    {
                        onExpiryMessage(message);
                    }
                });
            }

            public void Dispose()
            {
                _subscriber.UnsubscribeAll();
            }
        }
    }
}