* Add caching support on Redis
* Other improvements:
* Make sure we are using the cluster fully (all nodes) on AWS
* Put the global Redis Stuff into their own autofac module
* Add reliable transaction support (check every sub-operation task Statuses)
* Add Keyed Indexes (= excluding payload)
* Refactor the indexing api (separate read/write)
* Improve serialization performances
* Unify ES1 & ES2 Redis collections
* Make the collections names being configurable from the configuration
* Remove the dependency between the Expiry Listeners and the CacheStores
