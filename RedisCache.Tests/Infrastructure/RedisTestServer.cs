using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PEL.Framework.Redis.IntegrationTests.Infrastructure
{
    public class RedisTestServer : IDisposable
    {
        private readonly string _clientExePath;
        private readonly string _serverExePath;
        private readonly int? _port;
        private readonly string _password;
        private Process _serverProcess;
        private Process _clientConfigureProcess;

        public RedisTestServer(string redisFolder, int? port = null, string password = null)
        {
            _serverExePath = Path.Combine(redisFolder, "redis-server");
            _clientExePath = Path.Combine(redisFolder, "redis-cli");
            _port = port;
            _password = password;
        }

        // kill all instances
        public static void KillAll()
        {
            foreach (var process in Process.GetProcessesByName("redis-server"))
            {
                process.Kill();
            }
        }

        public async Task Start()
        {
            var args = string.Empty;
            if (_port != null)
            {
                args += $" --port {_port}";
            }

            _serverProcess = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = _serverExePath,
                    Arguments = args
                }
            };


            _clientConfigureProcess = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false, RedirectStandardOutput = true, FileName = _clientExePath, Arguments = " CONFIG SET notify-keyspace-events xKE"
                }
            };

            _serverProcess.Start();

            // wait 2 sec for the server to start
            await Task.Delay(2000);

            _clientConfigureProcess.Start();
        }

        public void Dispose()
        {
            _serverProcess?.Close();
            _clientConfigureProcess?.Close();
        }
    }
}