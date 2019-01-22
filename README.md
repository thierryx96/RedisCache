# Redis……..


## TL;DR (to install, configure and start Redis on your box)

1. Download the latest Redis Windows Distribution from: https://github.com/MSOpenTech/redis/releases. Once run this will install Redi as a Windows Service and start it.
2. (optional) : Install the Redis Browser from: https://redisdesktop.com/. The Redis Desktop Manager allows your to browse your Redis Instance data, etc ...
3. Run the Redis Command Line Client from : `C:\Program Files\Redis\redis-cli`. Run the command: `CONFIG SET notify-keyspace-events xKE` on it. This will enable the pub/sub mechanism for key expiry on Redis.



## Tools

* **Redis Windows Distribution (Server & Client)** : AWS Redis Server Version 3.2.4 (Enhanced) : https://github.com/MSOpenTech/redis/releases (x64) - 32 can be built from the source located on this repository.
* **Redis C# Client** : Stack Exchange : https://github.com/StackExchange/StackExchange.Redis
* **Redis Desktop manager** : https://redisdesktop.com/

## References

* **Redis Cheat sheet** https://www.cheatography.com/tasjaevan/cheat-sheets/redis/
* **Redis Recipes** https://github.com/rediscookbook/rediscookbook

* **Redis C# Cache Example** http://www.codefluff.com/using-redis-cache-with-net-and-c/
* **Redis C# Cache Example for MVC** https://ruhul.wordpress.com/2014/07/23/use-redis-as-cache-provider
* **Redis Pub/Sub** http://toreaurstad.blogspot.com.au/2015/09/synchronizing-redis-local-caches-for.html

* **Cache Invalidation** http://stackoverflow.com/questions/1188587/cache-invalidation-is-there-a-general-solution
* **Cache Invalidation methods** http://sorentwo.com/2016/08/01/strategies-for-selective-cache-expiration.html

* **AWS Shards** http://docs.aws.amazon.com/AmazonElastiCache/latest/UserGuide/Shards.html

## Data Structures

http://redis.io/topics/data-types-intro

| Structure type    | What it contains                                                             | Structure read/write ability                                                                                              |
|-------------------|------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| STRING            | Strings, integers, or floating-point values                                  | Operate on the whole string, parts, increment/ decrement the integers and floats                                          |
| LIST              | Linked list of strings                                                       | Push or pop items from both ends, trim based on offsets, read individual or multiple items, find or remove items by value |
| SET               | Unordered collection of unique strings                                       | Add, fetch, or remove individual items, check membership, intersect, union, difference, fetch random items                |
| HASH              | Unordered hash table of keys to values                                       | Add, fetch, or remove individual items, fetch the whole hash                                                              |
| ZSET (sorted set) | Ordered mapping of string members to floating-point scores, ordered by score | Add, fetch, or remove individual values, fetch items based on score ranges or member value                                |

### Strings

```
"key" -> "door"
"int" -> 1
"dec" -> 0.1
```

* Fast read by key, fast write
* Expiry on a single key
* Use case: retrieval by unique id


### Hashes

```
"companies" -> { "google" -> "blah", "yahoo" -> "bleh", ... }
"fruits" -> { "banana" -> "yellow", "apple" -> "red", "orange" -> "orange", ... }
```

* Fast read by (hashset's key, hash enty's key)
* Expiry on the full hashset
* Use case: retrieval by unique id, get a finite collection (get all)

### Sets

```
"tempeature" -> { "fahrenheit", "celsius",  "..." }
"companiesbycategory[it]" -> { "google", "yahoo"  }
```

* Set operations (union, intersects ... )
* Expiry on the set
* Use case: grouping things

### Sorted Sets


* Retrieve by rank (score) or by value. 
* Expiry on the set
* Use case: indexing by score (date time range etc ...), fetch by slicing

### List

```
"news" -> { "today", "yesterday",  "..." }
```

* Retrieval from head or tail = Java Linkedlist
* Expiry on the set
* Use case: threads, facebook posts ...






## Key Expiry Events

http://www.redis.io/topics/notifications

Keys with a time to live associated are expired by Redis in two ways:

When the key is accessed by a command and is found to be expired.
Via a background system that looks for expired keys in background, incrementally, in order to be able to also collect keys that are never accessed.

The expired events are generated when a key is accessed and is found to be expired by one of the above systems, as a result there are no guarantees that the Redis server will be able to generate the expired event at the time the key time to live reaches the value of zero. If no command targets the key constantly, and there are many keys with a TTL associated, there can be a significant delay between the time the key time to live drops to zero, and the time the expired event is generated.

**Enabling KeySpace notifications (expiry only)**

```
CONFIG SET notify-keyspace-events xKE
CONFIG GET notify-keyspace-events 
```

**All events (makes the whole system really chatty, not recommended for production systems)**

```
CONFIG SET notify-keyspace-events AKE 
CONFIG GET notify-keyspace-events 
```


**Test it with redis-cli (open a listener to event on db 0)**

```
PSUBSCRIBE __keyspace@0__:*   

PSUBSCRIBE __keyevents@<database>__:<command pattern> 
```

**With the StackExchange client (create and open a listener to events on db 0)**

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
                // Do stuff if some item is added to a hypothetical "users" set in Redis
            }
        }
    );
}
```

## AWS Elasticache Redis vs Standalone Redis Distribution 
The AWS Elasticache Redis configuration is really similar to a "non-cloud" Redis server system.

Main differences : 

* **Access control** is done through AWS own security layer : Cache Security Groups, VPN, Security Groups ... basically AWS allows client to access via authorized via security groups
* **Redis Server Configuration** is done through predefined customizable parameters groups, instead of configuration file (as per Redis classic). Therefore changing the config with the Redis-cli with commands such as *CONFIG SET notify-keyspace-events xKE* is not allowed on an AWS instance.

## Scalability / clustering features

http://stackoverflow.com/questions/2139443/redis-replication-and-redis-sharding-cluster-difference?answertab=active#tab-top

In simple words, the fundamental difference between the two concepts is that Sharding is used to scale Writes while Replication is used to scale Reads. As Alex already mentioned, Replication is also one of the solutions to achieve HA.

![](http://docs.aws.amazon.com/AmazonElastiCache/latest/UserGuide/images/ElastiCacheClusters-CSN-RedisShards.png)

Sharding, also known as partitioning, is splitting the data up by key; While replication, also know as mirroring, is to copy all data. Sharding is almost replication's antithesis, though they are orthogonal concepts and work well together.

Sharding is useful to increase performance, reducing the hit and memory load on any one resource. Replication is useful for high availability of reads. If you read from multiple replicas, you will also reduce the hit rate on all resources, but the memory requirement for all resources remains the same. It should be noted that, while you can write to a slave, replication is master->slave only. So you cannot scale writes this way.

Suppose you have the following tuples: [1:Apple], [2:Banana], [3:Cherry], [4:Durian] and we have two machines A and B. With Sharding, we might store keys 2,4 on machine A; and keys 1,3 on machine B. With Replication, we store keys 1,2,3,4 on machine A and 1,2,3,4 on machine B.

Sharding is typically implemented by performing a consistent hash upon the key. The above example was implemented with the following hash function h(x){return x%2==0?A:B}.

To combine the concepts, We might replicate each shard. In the above cases, all of the data (2,4) of machine A could be replicated on machine C and all of the data (1,3) of machine B could be replicated on machine D.

Any key-value store (of which Redis is only one example) supports sharding, though certain cross-key functions will no longer work: 

* Performing union and intersection operations over List/Set/Sorted Set data types across multiple shards and nodes
* Maintaining consistency across multi-shard/multi-node architecture, while running (a) a SORT command over a List of Hash keys; or (b) a Redis transaction that includes multiple keys; or (c) a Lua script with multiple keys
* Creating a simple abstraction layer that hides the complex cluster architecture from the user’s application, without code modifications and while supporting infinite scalability
* Maintaining a reliable and consistent infrastructure in a cluster configuration

Redis Cluster supports multiple key operations as long as all the keys involved into a single command execution (or whole transaction, or Lua script execution) all belong to the same hash slot. The user can force multiple keys to be part of the same hash slot by using a concept called hash tags.

**AWS Replication**

On AWS, by scaling up the Redis configuration, the number of **nodes** will increases, each node represent a replicated copy of the Redis Instance.

**AWS Sharding**

http://docs.aws.amazon.com/AmazonElastiCache/latest/UserGuide/Shards.html

On AWS, Redis configuration needs to be **cluster mode enabled**. Sharding can be used on top of replication. 


## Transactions 

Redis support transactions, as per definition, a transaction guarantee that no other write operation will occurs during while the transaction is pending.

*Simplest demonstration using the C# client*

```C#
    // start the transaction
	var transaction = _database.CreateTransaction();

	// add an operation to the transaction 
	// Resharper will complain as the operation bundle in the transaction are awaitable not being explicitly awaited 
	// As explained in details : https://github.com/StackExchange/StackExchange.Redis/blob/master/Docs/Transactions.md)	
	// Using #pragma warning disable 4014 will shut it. 
	transaction.StringSetAsync("myKey1", "val1");
	transaction.StringSetAsync("myKey2", "val2");
		
	await transaction.ExecuteAsync();
```

Transaction are per Redis instance. For distributed transaction see next chapter.

## Distributed Locking (not implemented yet)

https://github.com/samcook/RedLock.net

Distributed locks are useful for ensuring only one process is using a particular resource at any given time (even if the processes are running on different machines).

Redlock is a library that help achieving that, it has a good integration with the redis C# StackExchange Client

## Commands scripting

This is done by lua, and it is really well explained there : https://www.compose.com/articles/a-quick-guide-to-redis-lua-scripting/

