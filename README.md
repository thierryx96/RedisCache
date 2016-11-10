[![Build Status](https://travis-ci.org/thierryx96/RedisCache.svg?branch=master)](https://travis-ci.org/thierryx96/RedisCache)

# RedisCache

## Tools

* Redis Server : AWS Redis Server Version 3.2.4 (Enhanced) : https://github.com/MSOpenTech/redis/releases (x64) - 32 can be built from the source located on this repository.
* Redis C# Client : Stack Exchange : https://github.com/StackExchange/StackExchange.Redis
* Redis Desktop manager : https://redisdesktop.com/

## References

* Redis Cheatsheet : https://www.cheatography.com/tasjaevan/cheat-sheets/redis/
* Redis C# Cache Example : http://www.codefluff.com/using-redis-cache-with-net-and-c/
* Redis C# Cache Example for MVC :https://ruhul.wordpress.com/2014/07/23/use-redis-as-cache-provider/
* Redis Pub/Sub : http://toreaurstad.blogspot.com.au/2015/09/synchronizing-redis-local-caches-for.html
* Cache Invalidation : http://stackoverflow.com/questions/1188587/cache-invalidation-is-there-a-general-solution
* Cache Invalidation methods : http://sorentwo.com/2016/08/01/strategies-for-selective-cache-expiration.html

## TODO

* Indexes

## Key Expiry Events

http://www.redis.io/topics/notifications

Keys with a time to live associated are expired by Redis in two ways:

When the key is accessed by a command and is found to be expired.
Via a background system that looks for expired keys in background, incrementally, in order to be able to also collect keys that are never accessed.

The expired events are generated when a key is accessed and is found to be expired by one of the above systems, as a result there are no guarantees that the Redis server will be able to generate the expired event at the time the key time to live reaches the value of zero. If no command targets the key constantly, and there are many keys with a TTL associated, there can be a significant delay between the time the key time to live drops to zero, and the time the expired event is generated.

Enabling KeySpace notifications (expiry only)

```
CONFIG SET notify-keyspace-events xKE
CONFIG GET notify-keyspace-events 
```

All events (the former dos not seems to work)
```
CONFIG SET notify-keyspace-events AKE 
CONFIG GET notify-keyspace-events 
```

Test it with Redis Cli
```
PSUBSCRIBE __keyspace@0__:*
```

C# (StackExchange)

```
__keyevents@<database>__:<command> 
```

```C#
// Using stackExchange.Redis

using (ConnectionMultiplexer connection = ConnectionMultiplexer.Connect("localhost"))
{
    IDatabase db = connection.GetDatabase();
    ISubscriber subscriber = connection.GetSubscriber();

    subscriber.Subscribe("__keyspace@0__:*", (channel, value) =>
        {
            if ((string)channel == "__keyspace@0__:users" && (string)value == "sadd")
            {
                // Do stuff if some item is added to a hypothethical "users" set in Redis
            }
        }
    );
}
```


## AWS





## Redis Scritping

https://www.compose.com/articles/a-quick-guide-to-redis-lua-scripting/



