using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Cryptography;
using System.Web;
using System.Globalization;
using Nugget.Server.Extensions;

namespace Nugget.Server
{
    /// <summary>
    /// Handles the handshaking between the client and the host, when a new connection is created
    /// </summary>
    class HandshakeHandler
    {
        private Action<ClientHandshake> _onSuccess;
        private const int _bufferSize = 1024;

        public string Origin { get; set; }
        public string Location { get; set; }
        public ClientHandshake ClientHandshake { get; set; }
                        
        public HandshakeHandler(string origin, string location)
        {
            Origin = origin;
            Location = location;
        }

        /// <summary>
        /// Shake hands with the connecting socket
        /// </summary>
        /// <param name="socket">The socket to send the handshake to</param>
        /// <param name="callback">a callback function that is called when the send has completed</param>
        public void Shake(Socket socket, Action<ClientHandshake> callback)
        {
            _onSuccess = callback;
            try
            {
                
                var buffer = new byte[_bufferSize];

                socket.AsyncReceive(buffer, (size) => 
                {
                    if (size > 0)
                    {
                        var validShake = IsValidHandshake(buffer, size);
                        if (validShake)
                        {
                            // generate a response for the client
                            var serverShake = GenerateResponseHandshake();
                            // send the handshake to the client
                            BeginSendServerHandshake(serverShake, socket);
                        }
                        else
                        {
                            // the client shake isn't valid
                            Log.Debug("invalid handshake received from " + socket.LocalEndPoint);
                            socket.Close();
                        }
                    }
                });
  
            }
            catch (Exception e)
            {
                Log.Error("Exception thrown from method Receive:\n" + e.Message);
            }
        }

        private bool IsValidHandshake(byte[] buffer, int size)
        {

            // parse the client handshake and generate a response handshake
            var handshakeString = Encoding.ASCII.GetString(buffer, 0, size);
            ClientHandshake = new ClientHandshake(handshakeString);

            // check if the information in the client handshake is valid
            // TODO 404 on invalid location and 403 on invalid origin
            if (ClientHandshake.IsValid() && "ws://" + ClientHandshake.Host == Location && ClientHandshake.Origin == Origin)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private ServerHandshake GenerateResponseHandshake()
        {
           
            var acceptString = CalculateAcceptString(ClientHandshake.Key);

            var responseHandshake = new ServerHandshake()
            {
                Accept = acceptString,
                Location = "ws://" + ClientHandshake.Host + ClientHandshake.ResourceName.ToString(),
                Origin = ClientHandshake.Origin,
                SubProtocol = ClientHandshake.SubProtocol,
            };

            return responseHandshake;
        }
        
        private void BeginSendServerHandshake(ServerHandshake handshake, Socket socket)
        {
            var stringShake = "HTTP/1.1 101 Switching Protocols\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Accept: " + handshake.Accept + "\r\n"; 

            if (!string.IsNullOrEmpty(handshake.SubProtocol))
            {
                stringShake += "Sec-WebSocket-Protocol: " + handshake.SubProtocol + "\r\n";
            }
            stringShake += "\r\n";

            // generate a byte array representation of the handshake
            byte[] byteResponse = Encoding.ASCII.GetBytes(stringShake);

            socket.AsyncSend(byteResponse, (size) =>
            {
                if (_onSuccess != null)
                    _onSuccess(ClientHandshake);
            });
        }

        private static string CalculateAcceptString(string key)
        {
            // the following code is to conform with the protocol

            var concatString = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            var accept = key + concatString;
            using (var sha = System.Security.Cryptography.SHA1.Create())
            {
                var iba = sha.ComputeHash(Encoding.ASCII.GetBytes(accept));
                accept = Convert.ToBase64String(iba);
            }
            return accept;
        }
        
    }
}
