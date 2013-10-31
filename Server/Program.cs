using System;
using CommandLine;
using Common.Logging;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using SharedWeb;

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
            AppDomain.CurrentDomain.Load(typeof (MyHub).Assembly.FullName);

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
            app.Map("/signalr", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                var hubConfiguration = new HubConfiguration
                {
                    // You can enable JSONP by uncommenting line below.
                    // JSONP requests are insecure but some older browsers (and some
                    // versions of IE) require JSONP to work cross domain
                    // EnableJSONP = true
                };
                map.RunSignalR(hubConfiguration);
            });
        }
    }
}