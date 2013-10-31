using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Common.Logging;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Shared;

namespace Client
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> RepateCreate<T>(Func<T> constructor, int count)
        {
            return Enumerable.Repeat(constructor, count).Select(c => c());
        }
    }

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

            Thread.Sleep(1000);
            var program = new Program(options);
            program.Run();
        }

        public void Run()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var consumers = EnumerableExtensions.RepateCreate(() => StartConsumer(cancellationTokenSource.Token), _options.Consumers).ToArray();
            Log.InfoFormat("Started {0} consumers", _options.Consumers);

            var producers = EnumerableExtensions.RepateCreate(() => StartProducer(cancellationTokenSource.Token), _options.Producers).ToArray();
            Log.InfoFormat("Started {0} producers", _options.Producers);

            Console.ReadLine();

            cancellationTokenSource.Cancel();

            Task.WaitAll(producers);
            Log.Info("Producers stopped");

            Task.WaitAll(consumers);
            Log.Info("Consumers stopped");

            foreach (var p in producers)
            {
                Log.Info(p.Result);
            }

            Log.InfoFormat("Total Producer metrics: {0}", new ProducerMetric(null, producers.First().Result.Transport, producers.Sum(p => p.Result.Count)));

            foreach (var c in consumers)
            {
                Log.Info(c.Result);
            }

            Log.InfoFormat("Total Consumer metrics: {0}", new ConsumerMetric(null, consumers.First().Result.Transport, consumers.SelectMany(c => c.Result.Timespans), _options.DisplayFormat));
        }

        private IClientTransport GetClientTransport(Transport transport)
        {
            switch (transport)
            {
                case Transport.Auto:
                    return new AutoTransport(new DefaultHttpClient());
                case Transport.LongPolling:
                    return new LongPollingTransport();
                case Transport.ServerSentEvents:
                    return new ServerSentEventsTransport();
                case Transport.WebSockets:
                    return new WebSocketTransport();
                default:
                    throw new FormatException("Unknown transport");
            }
        }

        private async Task<Hub> Start()
        {
            try
            {
                var hubConnection = new HubConnection(_options.Host);
                var hubProxy = hubConnection.CreateHubProxy("MyHub");
                await hubConnection.Start(GetClientTransport(_options.Transport));
                return new Hub(hubConnection, hubProxy);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error establishing connection", ex);
                throw;
            }
        }

        private async Task<ConsumerMetric> StartConsumer(CancellationToken cancellationToken)
        {
            var hub = await Start();
            var list = new List<TimeSpan>();
            var connectionId = hub.Connection.ConnectionId;
            Log.InfoFormat("Starting Consumer {0}", connectionId);
            hub.Proxy.On<Message>("message", m => list.Add(DateTime.Now - m.DateTime));
            try
            {
                await Task.Delay(int.MaxValue, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            return new ConsumerMetric(connectionId, hub.Connection.Transport, list, _options.DisplayFormat);
        }

        private async Task<ProducerMetric> StartProducer(CancellationToken cancellationToken)
        {
            var count = 0;
            var hub = await Start();
            var body = new string('a', _options.Size);
            var connectionId = hub.Connection.ConnectionId;
            Log.InfoFormat("Starting Producer {0}", connectionId);
            try
            {
                do
                {
                    await hub.Proxy.Invoke("Send", new Message(DateTime.Now, body));
                    Interlocked.Increment(ref count);
                    await Task.Delay(1000/_options.Frequency, cancellationToken);
                } while (!cancellationToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return new ProducerMetric(connectionId, hub.Connection.Transport, count);
        }

        private class Hub
        {
            private readonly HubConnection _connection;
            private readonly IHubProxy _proxy;

            public Hub(HubConnection connection, IHubProxy proxy)
            {
                _connection = connection;
                _proxy = proxy;
            }

            public HubConnection Connection
            {
                get { return _connection; }
            }

            public IHubProxy Proxy
            {
                get { return _proxy; }
            }
        }

        private class ProducerMetric
        {
            private readonly string _connectionId;
            private readonly IClientTransport _transport;
            private readonly int _count;

            public ProducerMetric(string connectionId, IClientTransport transport, int count)
            {
                _connectionId = connectionId;
                _transport = transport;
                _count = count;
            }

            public string ConnectionId
            {
                get { return _connectionId; }
            }

            public IClientTransport Transport
            {
                get { return _transport; }
            }

            public int Count
            {
                get { return _count; }
            }

            public override string ToString()
            {
                return string.Format("ConnectionId: {0}, Transport: {1}, Count: {2}", ConnectionId, Transport, Count);
            }
        }

        private class ConsumerMetric
        {
            private readonly string _connectionId;
            private readonly IClientTransport _transport;
            private readonly List<TimeSpan> _timespans;
            private readonly string _format;

            public ConsumerMetric(string connectionId, IClientTransport transport, IEnumerable<TimeSpan> timespans, string format)
            {
                _connectionId = connectionId;
                _transport = transport;
                _timespans = timespans.ToList();
                _format = format;
                Max = _timespans.Max();
                Min = _timespans.Min();
                Count = _timespans.Count();
                long averageTicks = Convert.ToInt64(_timespans.Average(timeSpan => timeSpan.Ticks));
                Average = new TimeSpan(averageTicks);
            }

            public TimeSpan Average { get; set; }

            public TimeSpan Min { get; set; }

            public TimeSpan Max { get; set; }

            public int Count { get; set; }

            public string ConnectionId
            {
                get { return _connectionId; }
            }

            public IClientTransport Transport
            {
                get { return _transport; }
            }

            public List<TimeSpan> Timespans
            {
                get { return _timespans; }
            }

            public override string ToString()
            {
                return string.Format("ConnectionId: {0}, Transport: {1}, Count: {2}, Average: {3}, Min: {4}, Max: {5}", ConnectionId, Transport, Count, Format(Average), Format(Min), Format(Max));
            }

            private string Format(TimeSpan timeSpan)
            {
                if (string.Equals(_format, "m", StringComparison.InvariantCultureIgnoreCase))
                    return timeSpan.TotalMilliseconds.ToString();
                else if (string.Equals(_format, "m", StringComparison.InvariantCultureIgnoreCase))
                    return timeSpan.ToString("ss.\fffffff");
                else
                    return timeSpan.ToString();
            }
        }

        private class Options
        {
            [Option('p', "producers", DefaultValue = 5)]
            public int Producers { get; set; }

            [Option('c', "consumers", DefaultValue = 5)]
            public int Consumers { get; set; }

            [Option('f', "frequency", DefaultValue = 100)]
            public int Frequency { get; set; }

            [Option('s', "size", DefaultValue = 100)]
            public int Size { get; set; }

            [Option('h', "host", DefaultValue = "http://localhost:8123")]
            public string Host { get; set; }

            [Option('d', "displayformat", DefaultValue = "m")]
            public string DisplayFormat { get; set; }

            [Option('t', "transport", DefaultValue = Transport.Auto)]
            public Transport Transport { get; set; }

            public override string ToString()
            {
                return string.Format("Producers: {0}, Consumers: {1}, Frequency: {2}, Size: {3}, Host: {4}", Producers, Consumers, Frequency, Size, Host);
            }
        }

       
    }
}