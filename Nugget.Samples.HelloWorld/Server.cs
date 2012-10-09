using Nugget.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nugget.Samples.HelloWorld
{
    class Server
    {
        static void Main(string[] args)
        {

            var server = new WebSocketServer("ws://localhost:8181", "null");
            var sockets = new List<WebSocketConnection>();
            server.OnConnect += (socket) =>
            {
                sockets.Add(socket);
                Console.WriteLine("new connection");
                
                socket.OnReceive += (bytes) =>
                {
                    var data = Encoding.UTF8.GetString(bytes.ToArray());
                    Console.WriteLine("received: " + data);
                };

                socket.OnDisconnect += () =>
                {
                    Console.WriteLine("disconnected");
                    sockets.Remove(socket);
                };
            };

            server.Start();

            var cmd = "";
            while (cmd != "exit")
            {
                cmd = Console.ReadLine();
                sockets.ForEach(s => s.Send(cmd));
            }
        }
    }
}
