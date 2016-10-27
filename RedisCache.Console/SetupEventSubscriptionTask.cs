using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache.Console
{
    public class SetupEventSubscriptionTask : IStartUpTask
    {
        private readonly IRedisConnectionFactory _connectionFactory;
        private readonly IEventDispatcher _dispatcher;

        public SetupEventSubscriptionTask(IRedisConnectionFactory connectionFactory, IEventDispatcher dispatcher)
        {
            _connectionFactory = connectionFactory;
            _dispatcher = dispatcher;
        }

        public void Run()
        {
            var con = _connectionFactory.Get();

            var channel = new RedisChannel("event:book:changed:*", RedisChannel.PatternMode.Pattern);

            var subscriber = con.GetSubscriber();

            subscriber.Subscribe(channel,
                        (channel, value) =>
                        {
                            _dispatcher.Dispatch<CacheSourceChanged>(new CacheSourceChanged
                            {
                                Name = channel.ToString(), // name of the key
                            Value = value // value that passed. in this case id of the book
                        });
                        });
        }
    }
}
