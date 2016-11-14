using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.Database
{
    /// <summary>
    /// Utils methods on a Sever and Database level
    /// </summary>
    internal class RedisDatabaseManager
    {
        private readonly IRedisDatabaseConnector _connector;

        public RedisDatabaseManager(IRedisDatabaseConnector connector)
        {
            _connector = connector;
        }

        /// <summary>
        /// Flush all the keys on the database
        /// </summary>
        /// <returns></returns>
        public async Task FlushAll()
        {
            foreach (var endPoint in _connector.GetConnectedDatabase().Multiplexer.GetEndPoints())
            {
                await _connector.GetConnectedDatabase().Multiplexer.GetServer(endPoint).FlushDatabaseAsync(database: _connector.GetConnectedDatabase().Database);
            }
        }

        /// <summary>
        /// Search for the keys on a db given a pattern.
        /// Extremely inefficient : should be used with care, as it scan the entire key space
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public IEnumerable<string> ScanKeys(string pattern = "*")
        {
            return
                from endPoint in _connector.GetConnectedDatabase().Multiplexer.GetEndPoints()
                from key in _connector.GetConnectedDatabase().Multiplexer.GetServer(endPoint).Keys(database: _connector.GetConnectedDatabase().Database, pattern: pattern)
                select key.ToString();
        }
    }
}