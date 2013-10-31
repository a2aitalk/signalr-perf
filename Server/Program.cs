using System;
using System.Threading.Tasks;
using CommandLine;
using Common.Logging;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Shared;

namespace Server
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly Options _options;

        private Program(Options options)
        {
            _options = options;
        }

        private static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                return;
            }

            var program = new Program(options);
            program.Run();
        }

        public void Run()
        {
            using (WebApp.Start(_options.Host))
            {
                Log.InfoFormat("Server running on {0}", _options.Host);
                Console.ReadLine();
            }
        }

        private class Options
        {
            [Option('h', "host", DefaultValue = "http://localhost:8123")]
            public string Host { get; set; }

            public override string ToString()
            {
                return string.Format("Host: {0}", Host);
            }
        }
    }

    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Branch the pipeline here for requests that start with "/signalr"
            app.Map("/signalr", map =>
            {
                // Setup the CORS middleware to run before SignalR.
                // By default this will allow all origins. You can 
                // configure the set of origins and/or http verbs by
                // providing a cors options with a different policy.
                map.UseCors(CorsOptions.AllowAll);
                var hubConfiguration = new HubConfiguration
                {
                    // You can enable JSONP by uncommenting line below.
                    // JSONP requests are insecure but some older browsers (and some
                    // versions of IE) require JSONP to work cross domain
                    // EnableJSONP = true
                };
                // Run the SignalR pipeline. We're not using MapSignalR
                // since this branch already runs under the "/signalr"
                // path.
                map.RunSignalR(hubConfiguration);
            });
        }
    }

    public class MyHub : Hub
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public void Send(Message message)
        {
            Clients.All.message(message);
        }

        public override Task OnConnected()
        {
            Log.InfoFormat("Client {0} connected", Context.ConnectionId);
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            Log.InfoFormat("Client {0} disconnected", Context.ConnectionId);
            return base.OnDisconnected();
        }

        public override Task OnReconnected()
        {
            Log.InfoFormat("Client {0} reconnected", Context.ConnectionId);
            return base.OnReconnected();
        }
    }
}