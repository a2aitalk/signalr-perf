using System.Threading.Tasks;
using Common.Logging;
using Microsoft.AspNet.SignalR;
using Shared;

namespace SharedWeb
{
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