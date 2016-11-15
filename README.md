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

* AWS Shards : http://docs.aws.amazon.com/AmazonElastiCache/latest/UserGuide/Shards.html


## Key Expiry Events

http://www.redis.io/topics/notifications

Keys with a time to live associated are expired by Redis in two ways:

When the key is accessed by a command and is found to be expired.
Via a background system that looks for expired keys in background, incrementally, in order to be able to also collect keys that are never accessed.

The expired events are generated when a key is accessed and is found to be expired by one of the above systems, as a result there are no guarantees that the Redis server will be able to generate the expired event at the time the key time to live reaches the value of zero. If no command targets the key constantly, and there are many keys with a TTL associated, there can be a significant delay between the time the key time to live drops to zero, and the time the expired event is generated.

*Enabling KeySpace notifications (expiry only)*

```
CONFIG SET notify-keyspace-events xKE
CONFIG GET notify-keyspace-events 
```

*All events (makes the whole system really chatty, not recommanded for production systems)*
```
CONFIG SET notify-keyspace-events AKE 
CONFIG GET notify-keyspace-events 
```


*Test it with redis-cli (open a listener to event on db 0)*
```
PSUBSCRIBE __keyspace@0__:*   

PSUBSCRIBE __keyevents@<database>__:<command pattern> 
```

*With the StackExchange client (create and open a listener to events on db 0)*

```C#
// Using stackExchange.Redis

using (ConnectionMultiplexer connection = ConnectionMultiplexer.Connect("localhost"))
{
    IDatabase db = connection.GetDatabase();
    ISubscriber subscriber = connection.GetSubscriber();

    subscriber.Subscribe("__keyspace@0__:*", (channel, value) =>
        {
            if ((string)channel == "__keyspace@0__:users" && (string)value == "expriy")
            {
                // Do stuff if some item is added to a hypothethical "users" set in Redis
            }
        }
    );
}
```


## AWS Elasticache Redis vs Standalone Redis Distribution 

The AWS Elasticache Redis configuration is really similar to a "non-cloud" redis server system. 

Main differents point (related to our usage of it) : 

- **Access control** is done through AWS own security layer : Cache Security Groups, VPN, Security Groups ... basically AWS allows client to access via authorized via security groups
- **Redis Server Configuration** is done through predefined customizable paramters groups, instead of configuration file (as per redis classic). Therefore changing the config with the redis-cli with commands such as ```CONFIG SET notify-keyspace-events xKE``` is not allowed on an AWSinstance.

## Scalability features

In simple words, the fundamental difference between the two concepts is that Sharding is used to scale Writes while Replication is used to scale Reads. As Alex already mentioned, Replication is also one of the solutions to achieve HA.

![](http://docs.aws.amazon.com/AmazonElastiCache/latest/UserGuide/images/ElastiCacheClusters-CSN-RedisShards.png)

### Replication

* On AWS : by scaling up the redis configuration, the number of **nodes** will increases, each node represent a replicated copy of the Redis Instance

### Sharding 

* On AWS : Redis configuration needs to be **cluster mode enabled** 
* Ref : http://stackoverflow.com/questions/2139443/redis-replication-and-redis-sharding-cluster-difference?answertab=active#tab-top

Sharding, also known as partitioning, is splitting the data up by key; While replication, also know as mirroring, is to copy all data.

Sharding is almost replication's antithesis, though they are orthogonal concepts and work well together.

Sharding is useful to increase performance, reducing the hit and memory load on any one resource. Replication is useful for high availability of reads. If you read from multiple replicas, you will also reduce the hit rate on all resources, but the memory requirement for all resources remains the same. It should be noted that, while you can write to a slave, replication is master->slave only. So you cannot scale writes this way.

Suppose you have the following tuples: [1:Apple], [2:Banana], [3:Cherry], [4:Durian] and we have two machines A and B. With Sharding, we might store keys 2,4 on machine A; and keys 1,3 on machine B. With Replication, we store keys 1,2,3,4 on machine A and 1,2,3,4 on machine B.

Sharding is typically implemented by performing a consistent hash upon the key. The above example was implemented with the following hash function h(x){return x%2==0?A:B}.

To combine the concepts, We might replicate each shard. In the above cases, all of the data (2,4) of machine A could be replicated on machine C and all of the data (1,3) of machine B could be replicated on machine D.

Any key-value store (of which Redis is only one example) supports sharding, though certain cross-key functions will no longer work. Redis supports replication out of the box.


## Transactions 


## Commands scritping

This is done by lua. https://www.compose.com/articles/a-quick-guide-to-redis-lua-scripting/



