using StackExchange.Redis;
using System;

public interface IRedisConnectionWrapper : IDisposable
{
    IDatabase Database(int? db = null);
    IServer Server(EndPoint endPoint);
    EndPoint[] GetEndpoints();
    void FlushDb(int? db = null);
}


public class RedisConnectionWrapper : IRedisConnectionWrapper
{
    //private readonly ILogger _logger;
    private readonly Lazy<string> _connectionString;

    private volatile ConnectionMultiplexer _connection;
    private readonly object _lock = new object();

    public RedisConnectionWrapper(string connectionString)
    {
        _connectionString = connectionString;
        _logger = logger;

        _connectionString = new Lazy<string>(GetConnectionString);
    }

    private string GetConnectionString()
    {
        var con = ConfigurationManager.ConnectionStrings[_settings.ConnectionStringOrName];

        return con == null ? _settings.ConnectionStringOrName : con.ConnectionString;
    }

    private ConnectionMultiplexer GetConnection()
    {
        if (_connection != null && _connection.IsConnected) return _connection;

        lock (_lock)
        {
            if (_connection != null && _connection.IsConnected) return _connection;

            if (_connection != null)
            {
                _logger.Debug("Connection disconnected. Disposing connection...");
                _connection.Dispose();
            }

            _logger.Debug("Creating new instance of Redis Connection");
            _connection = ConnectionMultiplexer.Connect(_connectionString.Value);
        }

        return _connection;
    }

    public IDatabase Database(int? db = null)
    {
        return GetConnection().GetDatabase(db ?? _settings.DefaultDb);
    }

    public IServer Server(EndPoint endPoint)
    {
        return GetConnection().GetServer(endPoint);
    }

    public EndPoint[] GetEndpoints()
    {
        return GetConnection().GetEndPoints();
    }

    public void FlushDb(int? db = null)
    {
        var endPoints = GetEndpoints();

        foreach (var endPoint in endPoints)
        {
            Server(endPoint).FlushDatabase(db ?? _settings.DefaultDb);
        }
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.Dispose();
        }
    }
}