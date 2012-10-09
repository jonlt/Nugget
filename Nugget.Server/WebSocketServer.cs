using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace Nugget.Server
{
    public class WebSocketServer : IDisposable
    {
        /// <summary>
        /// The socket that listens for conenctions
        /// </summary>
        public Socket ListenerSocket { get; private set; }
        public string Location { get; private set; }
        public int Port { get; private set; }
        public string Origin { get; private set; }

        /// <summary>
        /// Event that is fired when a new client connects
        /// </summary>
        public event Action<WebSocketConnection> OnConnect;

        /// <summary>
        /// Instantiate a new web socket server
        /// </summary>
        /// <param name="location">the address and port of this web socket server (e.g. ws://localhost:8181)</param>
        /// <param name="origin">the address from where connections are allowed to come (e.g. http://localhost)</param>
        public WebSocketServer(string location, string origin)
        {
            Origin = origin;
            Location = location;
            Port = new Uri(location).Port;
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            // create the main server socket, bind it to the local ip address and start listening for clients
            ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);
            Log.Info("Server stated on " + ListenerSocket.LocalEndPoint);
            ListenForClients();
        }

        private void ListenForClients()
        {
            ListenerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }

        // a new client is trying to connect
        private void OnClientConnect(IAsyncResult ar)
        {
            Socket clientSocket = null;
            
            try
            {
                clientSocket = ListenerSocket.EndAccept(ar);
            }
            catch
            {
                Log.Error("Listener socket is closed");
                return;
            }
            
            var shaker = new HandshakeHandler(Origin, Location);
            // shake hands - and provide a callback for when hands has been shaken
            shaker.Shake(clientSocket, (handshake) =>
            {
                // instantiate the connection and subscribe to the events
                var wsc = new WebSocketConnection(clientSocket, handshake);
                
                // fire the connected event
                if (OnConnect != null)
                {
                    OnConnect(wsc);
                }

                // start looking for data
                wsc.StartReceiving();
            });
            
            // listen some more
            ListenForClients();
        }

        #region dispose
        protected virtual void Dispose(bool managed)
        {
            ListenerSocket.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}
